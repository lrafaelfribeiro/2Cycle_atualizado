using API.Core;
using System.Threading.RateLimiting;

namespace API.Extensions;

public static class RateLimiterExtensions
{
    private const int LoginPermitLimit = 10;
    private const int RegisterPermitLimit = 5;
    private const int RefreshPermitLimit = 10;
    private const int MapsPermitLimit = 120;

    private static readonly TimeSpan LoginWindow = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RegisterWindow = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan MapsWindow = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(
                RateLimitPolicyNames.AuthLogin,
                httpContext => CreateIpPolicy(
                    httpContext,
                    LoginPermitLimit,
                    LoginWindow));

            options.AddPolicy(
                RateLimitPolicyNames.AuthRegister,
                httpContext => CreateIpPolicy(
                    httpContext,
                    RegisterPermitLimit,
                    RegisterWindow));

            options.AddPolicy(
                RateLimitPolicyNames.AuthRefresh,
                httpContext => CreateIpPolicy(
                    httpContext,
                    RefreshPermitLimit,
                    RefreshWindow));

            options.AddPolicy(
                RateLimitPolicyNames.Maps,
                httpContext => CreateIpPolicy(
                    httpContext,
                    MapsPermitLimit,
                    MapsWindow));
        });

        return services;
    }

    private static RateLimitPartition<string> CreateIpPolicy(
        HttpContext httpContext,
        int permitLimit,
        TimeSpan window)
    {
        string partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }
}
