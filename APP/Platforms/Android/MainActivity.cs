using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace APP
{
    [Activity(Theme = "@style/MainTheme",
              MainLauncher = true,
              LaunchMode = LaunchMode.SingleTop,
              ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                Window.SetDecorFitsSystemWindows(false);

                // Não aplicar contraste automático na navigation bar 
                Window.NavigationBarContrastEnforced = false;
                
                 // Não aplicar contraste automático na status bar 
                Window.StatusBarContrastEnforced = false;
            }

            Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
            Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);

        }
    }
}
