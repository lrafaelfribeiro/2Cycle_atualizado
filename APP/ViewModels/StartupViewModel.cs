using APP.Services.Session;
using APP.Services.Sync;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace APP.ViewModels
{
    public partial class StartupViewModel : ObservableObject
    {
        private readonly ISessionService _sessionService;
        public StartupViewModel(ISessionService sessionService, IActivitySyncService activitySyncService)
        {
            _sessionService = sessionService;
            _ = activitySyncService.SyncPendingActivitiesAsync();
        }
        public async Task<bool> CheckSessionAsync()
        {
            try
            {
                return await _sessionService.EnsureSessionIsValidAsync(CancellationToken.None);
            }
            catch
            {
                return false;
            }
        }

        public async Task NavigateAsync(bool isSessionValid)
        {
            var route = isSessionValid ? "//main" : "//login";
            await Shell.Current.GoToAsync(route);
        }
    }
}