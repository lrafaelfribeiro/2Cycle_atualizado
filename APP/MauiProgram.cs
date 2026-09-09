using APP.Extensions;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using APP.Services.LocationTracking;


#if ANDROID
            using APP.Platforms.Android.Services;
#endif

namespace APP
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("Inter_18pt-Regular.ttf", "InterRegular");
                    fonts.AddFont("Inter_18pt-Medium.ttf", "InterMedium");
                    fonts.AddFont("Inter_18pt-Bold.ttf", "InterBold");
                    fonts.AddFont("Inter_18pt_ExtraBold.ttf", "InterExtraBold");
                });

            builder.Services.AddViews()
                            .AddViewModels()
                            .AddInfrastructure();

#if ANDROID
            builder.Services.AddSingleton<IForegroundLocationTrackingService, AndroidForegroundLocationTrackingService>();
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif


            return builder.Build();
        }
    }
}
