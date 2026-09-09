using System.Net.Security;
using APP.Core;
using APP.Services.Auth;
using APP.Services.Lifecycle;
using APP.Services.LocalStorage;
using APP.Services.Profile;
using APP.Services.Routes;
using APP.Services.Session;
using APP.Services.Sharing;
using APP.Services.Sync;
using APP.Services.Thumbnails;
using APP.Services.Tiles;
using APP.Services.Toast;
using APP.Services.Token;
using APP.Services.Weather;
using APP.ViewModels;
using APP.ViewModels.Activities;
using APP.Views;

namespace APP.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddViews(this IServiceCollection services)
        {
            // pages
            services.AddTransient<LoginPage>();
            services.AddTransient<RegisterPage>();
            services.AddTransient<StartupPage>();
            services.AddTransient<ChronometerPage>();
            services.AddTransient<ActivitySummaryPage>();
            services.AddTransient<CreateRoutePage>();
            services.AddTransient<RouteDetailPage>();

            // views
            services.AddTransient<HomeView>();
            services.AddTransient<ProfileView>();
            services.AddTransient<StartActivityView>();
            services.AddTransient<ChallengesView>();
            services.AddTransient<ActivitiesView>();
            services.AddTransient<RoutesView>();

            return services;
        }

        public static IServiceCollection AddViewModels(this IServiceCollection services)
        {
            services.AddTransient<LoginViewModel>();
            services.AddTransient<StartupViewModel>();
            services.AddTransient<HomeViewModel>();
            services.AddTransient<TrackingViewModel>();
            services.AddTransient<ActivitySummaryViewModel>();
            services.AddTransient<ActivitiesViewModel>();
            services.AddTransient<ProfileViewModel>();
            services.AddTransient<RegisterViewModel>();
            services.AddTransient<StartActivityViewModel>();
            services.AddTransient<CreateProfileViewModel>();
            services.AddTransient<RoutesViewModel>();
            services.AddTransient<CreateRouteViewModel>();
            services.AddTransient<RouteDetailViewModel>();

            return services;
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<IAppLifecycleService, AppLifecycleService>();

            services.ConfigureHttpClientDefaults(http =>
            {
                http.ConfigureHttpClient(httpClient =>
                {
                    httpClient.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                });
            });

            services.AddTransient<AuthHeaderHandler>();

            services.AddHttpClient<IAuthService, AuthService>();

            services.AddHttpClient<IProfileService, ProfileService>()
                    .AddHttpMessageHandler<AuthHeaderHandler>();

            services.AddHttpClient<IActivitySyncService, ActivitySyncService>()
                    .AddHttpMessageHandler<AuthHeaderHandler>();

            services.AddHttpClient<IWeatherService, WeatherService>(client =>
            {
                client.BaseAddress = new Uri("https://api.open-meteo.com/");
            });

            services.AddHttpClient<ITileCacheService, TileCacheService>(client =>
            {
                client.BaseAddress = new Uri("https://a.tile-cyclosm.openstreetmap.fr/cyclosm/");
            });

            services.AddSingleton<ITokenStorageService, TokenStorageService>();
            services.AddScoped<ISessionService, SessionService>();

            services.AddSingleton<IToastService, ToastService>();

            services.AddSingleton<ILocalDatabaseService, LocalDatabaseService>();
            services.AddScoped<IActivityLocalRepository, ActivityLocalRepository>();

            services.AddScoped<IUserContextService, UserContextService>();

            services.AddScoped<IShareCardService, ShareCardService>();

            services.AddHttpClient<IRouteService, RouteService>()
                    .AddHttpMessageHandler<AuthHeaderHandler>();

            services.AddScoped<IRouteThumbnailService, RouteThumbnailService>();

            return services;
        }

    }
}
