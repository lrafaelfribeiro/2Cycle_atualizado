using Android.Text;
using APP.Services.LocationTracking;
using APP.Services.Session;
using APP.Services.Toast;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace APP.ViewModels
{
    public partial class TrackingViewModel : ObservableObject,
        IRecipient<LocationTrackingUpdateMessage>,
        IRecipient<LocationTrackingStoppedMessage>,
        IRecipient<LocationTrackingAutoPauseChangedMessage>
    {
        private readonly IForegroundLocationTrackingService _trackingService;
        private readonly IUserContextService _userContextService;
        private readonly IToastService _toastService;

        private IDispatcherTimer? _totalTimeTimer;
        private DateTime _startedAt;
        private Guid _activityId;

        [ObservableProperty] private string totalTimeDisplay = "00:00:00";
        [ObservableProperty] private string movingTimeDisplay = "00:00:00";
        [ObservableProperty] private double distanceKm;
        [ObservableProperty] private double currentSpeedKmh;
        [ObservableProperty] private bool isTracking;
        [ObservableProperty] private bool isPaused;
        [ObservableProperty] private bool isAutoPaused;
        [ObservableProperty] private bool autoPauseEnabled = true;
        [ObservableProperty] private string primaryButtonText = "▶";
        [ObservableProperty] private double? currentElevationMeters;
        [ObservableProperty] private double elevationGainMeters;
        [ObservableProperty] private string paceDisplay = "--:--";
        [ObservableProperty] private bool headerVisible = true;
        private int _movingTimeSeconds;

        public TrackingViewModel(IForegroundLocationTrackingService trackingService, IUserContextService userContextService, IToastService toastService)
        {
            _trackingService = trackingService;
            _userContextService = userContextService;
            _toastService = toastService;
        }

        public void OnAppearing() => WeakReferenceMessenger.Default.RegisterAll(this);
        public void OnDisappearing() => WeakReferenceMessenger.Default.UnregisterAll(this);

        [RelayCommand]
        private async Task PrimaryActionAsync()
        {
            if (!IsTracking)
            {
                await StartTrackingAsync();
            }
            else if (IsPaused || IsAutoPaused)
            {
                await _trackingService.ResumeAsync();
                IsPaused = false;
                IsAutoPaused = false;
                PrimaryButtonText = "Ⅱ";
            }
            else
            {
                await _trackingService.PauseAsync();
                IsPaused = true;
                PrimaryButtonText = "▶";
            }
        }

        private async Task StartTrackingAsync()
        {
            var userId = await _userContextService.GetCurrentUserIdAsync();
            if (userId is null)
            {
                await _toastService.Show("Sessão inválida. Volta a entrar.");
                return;
            }

            _activityId = Guid.NewGuid();
            _startedAt = DateTime.UtcNow;
            _movingTimeSeconds = 0;

            bool started = await _trackingService.StartAsync(_activityId, userId, AutoPauseEnabled);
            if (!started)
            {
                await _toastService.Show("Ativa o GPS para iniciar a gravação.");
                return;
            }

            IsTracking = true;
            IsPaused = false;
            PrimaryButtonText = "Ⅱ";
            StartTotalTimeTimer();
        }

        [RelayCommand]
        private async Task StopAsync()
        {
            if (!IsTracking) return;

            await _trackingService.StopAsync();
            StopTotalTimeTimer();

            IsAutoPaused = false;
            IsTracking = false;
            IsPaused = false;
        }

        private void StartTotalTimeTimer()
        {
            _totalTimeTimer = Application.Current!.Dispatcher.CreateTimer();
            _totalTimeTimer.Interval = TimeSpan.FromSeconds(1);
            _totalTimeTimer.Tick += (_, _) =>
            {
                if (IsPaused || IsAutoPaused) return; // não incrementa localmente durante pausa manual OU automática

                _movingTimeSeconds++;
                MovingTimeDisplay = TimeSpan.FromSeconds(_movingTimeSeconds).ToString(@"hh\:mm\:ss");
            };
            _totalTimeTimer.Start();
        }

        private void StopTotalTimeTimer()
        {
            _totalTimeTimer?.Stop();
            _totalTimeTimer = null;
        }

        public void Receive(LocationTrackingUpdateMessage message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DistanceKm = message.DistanceMeters / 1000.0;
                CurrentSpeedKmh = message.CurrentSpeedKmh;
                CurrentElevationMeters = message.AltitudeMeters;
                ElevationGainMeters = message.ElevationGainMeters;

                _movingTimeSeconds = message.MovingTimeSeconds;
                MovingTimeDisplay = TimeSpan.FromSeconds(_movingTimeSeconds).ToString(@"hh\:mm\:ss");

                PaceDisplay = DistanceKm > 0.05
                    ? FormatPace(_movingTimeSeconds / 60.0 / DistanceKm)
                    : "--:--";
            });
        }

        public void Receive(LocationTrackingAutoPauseChangedMessage message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsAutoPaused = message.IsAutoPaused;
                if (message.IsAutoPaused)
                {
                    PrimaryButtonText = "▶";
                }
                else
                {
                    PrimaryButtonText = "Ⅱ";
                }
            });
        }

        public void Receive(LocationTrackingStoppedMessage message)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                StopTotalTimeTimer();

                IsTracking = false;
                IsPaused = false;
                IsAutoPaused = false;

                if (!message.Success)
                    return;

                await Shell.Current.GoToAsync($"../activity-summary?activityId={message.ActivityId}");
            });
        }

        private static string FormatPace(double minutesPerKm)
        {
            int minutes = (int)minutesPerKm;
            int seconds = (int)Math.Round((minutesPerKm - minutes) * 60);
            return $"{minutes:00}:{seconds:00}";
        }

        partial void OnAutoPauseEnabledChanged(bool value)
        {
            if (IsTracking) _ = _trackingService.SetAutoPauseAsync(value);
        }

        partial void OnIsTrackingChanged(bool value)
        {
            HeaderVisible = !value;
        }
    }
}
