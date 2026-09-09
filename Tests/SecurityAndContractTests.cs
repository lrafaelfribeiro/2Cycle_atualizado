using System.Reflection;
using System.Text.Json;
using API.Controllers;
using API.Data;
using API.DTOs;
using API.Models;
using API.Services.Activities;
using API.Services.OpenRouteService;
using API.Services.RefreshTokens;
using API.Services.Routes;
using API.Services.Token;
using API.Validators;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests;

public sealed class SecurityAndContractTests
{
    [Fact]
    public async Task Route_detail_is_not_available_to_a_different_user()
    {
        await using var database = await TestDatabase.CreateAsync();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var routeId = Guid.NewGuid();
        var suggestedRouteId = Guid.NewGuid();

        database.Context.Users.Add(new User { Id = ownerId, Name = "Owner", Email = "owner@example.com", PasswordHash = "hash" });
        database.Context.SuggestedRoutes.Add(new SuggestedRoute { Id = suggestedRouteId, DistanceMeters = 1000 });
        database.Context.UserSuggestedRoutes.Add(new UserSuggestedRoute
        {
            Id = routeId,
            UserId = ownerId,
            SuggestedRouteId = suggestedRouteId,
            Name = "Privada"
        });
        await database.Context.SaveChangesAsync();

        var service = new RouteSuggestionService(database.Context, new StubRouteClient());
        var result = await service.GetRouteDetailAsync(otherUserId, routeId);

        Assert.True(result.IsFailure);
        Assert.Equal("ROUTE_NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public void Profile_controller_does_not_expose_a_user_id_route()
    {
        var routes = typeof(ProfilesController).GetMethods()
            .Select(method => method.GetCustomAttribute<HttpMethodAttribute>()?.Template)
            .Where(template => template is not null);

        Assert.DoesNotContain("{id:guid}", routes);
    }

    [Fact]
    public void Route_contract_exposes_round_trip_and_point_altitude()
    {
        var response = new SavedRouteSummaryResponse(Guid.NewGuid(), Guid.NewGuid(), "R", 1, 2, true, false, DateTime.UtcNow, []);
        var point = new RoutePointResponse(1, 2, 3, 0);
        var json = JsonSerializer.Serialize(new { response, point }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("isRoundTrip", json);
        Assert.Contains("altitudeMeters", json);
        Assert.DoesNotContain("elevationGainMeters\":3", json);
    }

    [Fact]
    public async Task Activity_validator_rejects_invalid_track_data()
    {
        var request = new CreateActivityRequest(
            Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddMinutes(-1), -1, -1, 1, -1, -1, -1,
            (MoodRating)99,
            [new CreateTrackSegmentRequest(-1, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(-1), [])]);

        var result = await new CreateActivityRequestValidator().ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Route_and_profile_validators_reject_invalid_values()
    {
        var routeResult = await new RouteSuggestionRequestValidator().ValidateAsync(new RouteSuggestionRequest(double.NaN, 0, 0, 0));
        var profileResult = await new CreateProfileRequestValidator().ValidateAsync(new CreateProfileRequest(
            new DateOnly(2000, 1, 1), 170, 70, 65, 3, (Sex)99));

        Assert.False(routeResult.IsValid);
        Assert.False(profileResult.IsValid);
    }

    [Fact]
    public async Task Activity_creation_is_idempotent_for_the_same_user()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new ActivityService(database.Context);
        var request = ValidActivity(Guid.NewGuid());
        var userId = Guid.NewGuid();
        database.Context.Users.Add(new User { Id = userId, Name = "Cyclist", Email = "cyclist@example.com", PasswordHash = "hash" });
        await database.Context.SaveChangesAsync();

        var first = await service.CreateAsync(userId, request);
        var second = await service.CreateAsync(userId, request);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, await database.Context.Activities.CountAsync());
    }

    [Fact]
    public async Task A_refresh_token_cannot_be_rotated_twice()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = new User { Id = Guid.NewGuid(), Name = "User", Email = "user@example.com", PasswordHash = "hash" };
        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();

        var tokenService = new StubTokenService();
        var service = new RefreshTokenService(database.Context, tokenService);
        var rawToken = await service.CreateAsync(user.Id);

        var first = await service.RotateAsync(rawToken);
        var second = await service.RotateAsync(rawToken);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal("REVOKED_TOKEN", second.Error!.Code);
    }

    private static CreateActivityRequest ValidActivity(Guid id) => new(
        id, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, 1, 60, 60, 1, 1, 0, MoodRating.Good,
        [new CreateTrackSegmentRequest(0, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow,
            [new CreateTrackPointRequest(0, 0, 0, null, DateTime.UtcNow)])]);

    private sealed class StubRouteClient : IOpenRouteServiceClient
    {
        public Task<ORSRouteResult> GetCyclingRouteAsync(double originLat, double originLon, double destLat, double destLon, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ORSRouteResult> GetRoundTripRouteAsync(double originLat, double originLon, double desiredLengthMeters, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class StubTokenService : ITokenService
    {
        public string GenerateAccessToken(User user) => "access-token";
        public string GenerateRefreshToken() => Guid.NewGuid().ToString("N");
        public string HashRefreshToken(string rawToken) => rawToken;
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public ApplicationDbContext Context { get; }

        private TestDatabase(SqliteConnection connection, ApplicationDbContext context)
        {
            _connection = connection;
            Context = context;
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
            await context.Database.EnsureCreatedAsync();
            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
