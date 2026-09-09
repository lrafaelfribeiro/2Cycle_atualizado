using API.DTOs;
using API.DTOs.Actitivities.Requests;
using API.DTOs.Routes.Requests;
using API.Handlers;
using API.Services.Activities;
using API.Services.Auth;
using API.Services.Caching;
using API.Services.OpenRouteService;
using API.Services.Profiles;
using API.Services.RefreshTokens;
using API.Services.Routes;
using API.Services.Token;
using API.Services.Users;
using API.Validators.Activity;
using API.Validators.Auth;
using API.Validators.Profile;
using API.Validators.Profile.Weight;
using API.Validators.Route;
using FluentValidation;

namespace API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IActivityService, ActivityService>();
            services.AddScoped<IRouteSuggestionService, RouteSuggestionService>();
            services.AddScoped<ISavedRouteService, SavedRouteService>();
            services.AddSingleton<IPendingRouteCache, PendingRouteCache>();

            services.AddTransient<ORSApiKeyHandler>();
            services.AddHttpClient<IOpenRouteServiceClient, OpenRouteServiceClient>((sp, client) =>
            {
                client.BaseAddress = new Uri("https://api.openrouteservice.org/");
            })
            .AddHttpMessageHandler<ORSApiKeyHandler>();

            return services;
        }

        public static IServiceCollection AddValidators(this IServiceCollection services)
        {
            services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
            services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
            services.AddScoped<IValidator<CreateProfileRequest>, CreateProfileRequestValidator>();
            services.AddScoped<IValidator<UpdateProfileRequest>, UpdateProfileRequestValidator>();
            services.AddScoped<IValidator<AddWeightLogRequest>, AddWeightLogRequestValidator>();
            services.AddScoped<IValidator<RouteSuggestionRequest>, RouteSuggestionRequestValidator>();
            services.AddScoped<IValidator<RouteRoundTripSuggestionRequest>, RouteRoundTripSuggestionRequestValidator>();
            services.AddScoped<IValidator<CreateActivityRequest>, CreateActivityRequestValidator>();

            return services;
        }

    }
}
