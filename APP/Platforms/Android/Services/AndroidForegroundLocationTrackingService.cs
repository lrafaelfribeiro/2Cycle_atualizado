using Android.Content;
using Android.Locations;
using Android.OS;
using APP.Services.LocationTracking;

namespace APP.Platforms.Android.Services
{
    public class AndroidForegroundLocationTrackingService : IForegroundLocationTrackingService
    {
        public bool IsTracking { get; private set; }

        public Task<bool> StartAsync(Guid activityId, string ownerUserId, bool autoPauseEnabled)
        {
            var locationManager = (LocationManager)global::Android.App.Application.Context.GetSystemService(Context.LocationService)!;
            if (!locationManager.IsProviderEnabled(LocationManager.GpsProvider))
                return Task.FromResult(false);

            var intent = new Intent(global::Android.App.Application.Context, typeof(LocationForegroundService));
            intent.SetAction(LocationForegroundService.ActionStart);
            intent.PutExtra(LocationForegroundService.ExtraActivityId, activityId.ToString());
            intent.PutExtra(LocationForegroundService.ExtraOwnerUserId, ownerUserId);
            intent.PutExtra(LocationForegroundService.ExtraAutoPauseEnabled, autoPauseEnabled);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                global::Android.App.Application.Context.StartForegroundService(intent);
            }
            else
            {
                global::Android.App.Application.Context.StartService(intent); // API < 26: StartService normal, o próprio Service chama StartForeground() a seguir
            }

            IsTracking = true;
            return Task.FromResult(true);
        }

        public Task SetAutoPauseAsync(bool enabled)
        {
            var intent = new Intent(global::Android.App.Application.Context, typeof(LocationForegroundService));
            intent.SetAction(LocationForegroundService.ActionSetAutoPause);
            intent.PutExtra(LocationForegroundService.ExtraAutoPauseEnabled, enabled);
            global::Android.App.Application.Context.StartService(intent);
            return Task.CompletedTask;
        }

        public Task PauseAsync() => SendAction(LocationForegroundService.ActionPause);

        public Task ResumeAsync() => SendAction(LocationForegroundService.ActionResume);

        public async Task<Guid> StopAsync()
        {
            await SendAction(LocationForegroundService.ActionStop);
            IsTracking = false;
            return Guid.Empty; // o Id já é conhecido do lado do ViewModel, que o gerou no StartAsync
        }

        private Task SendAction(string action)
        {
            var intent = new Intent(global::Android.App.Application.Context, typeof(LocationForegroundService));
            intent.SetAction(action);
            global::Android.App.Application.Context.StartService(intent);
            return Task.CompletedTask;
        }
    }
}
