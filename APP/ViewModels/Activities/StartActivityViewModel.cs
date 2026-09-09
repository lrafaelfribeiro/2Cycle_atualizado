using System.Collections.ObjectModel;
using APP.Core.Messages;
using APP.Models;
using APP.Services.Weather;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace APP.ViewModels.Activities
{
    public partial class StartActivityViewModel : ObservableObject, IRecipient<AppResumedMessage>
    {
        private const int LocationTimeoutSeconds = 10;

        private readonly IWeatherService _weatherService;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartActivityCommand))]
        private bool isLocationAvailable;

        [ObservableProperty] private ObservableCollection<HourlyForecastItem> forecast = new();
        [ObservableProperty] private bool isLoadingWeather;
        [ObservableProperty] private bool isLoading;

        private bool _isInitializing;

        public StartActivityViewModel(IWeatherService weatherService)
        {
            _weatherService = weatherService;
            WeakReferenceMessenger.Default.Register(this);
        }

        public void Receive(AppResumedMessage message)
        {
            MainThread.BeginInvokeOnMainThread(() => _ = InitializeAsync());
        }

        public async Task InitializeAsync()
        {
            if (_isInitializing)
                return;

            _isInitializing = true;
            IsLoading = true;

            Microsoft.Maui.Devices.Sensors.Location? location = null;

            try
            {
                if (await AskLocationPermissionAsync())
                {
                    location = await GetCurrentLocationAsync();
                    IsLocationAvailable = location is not null;
                }
                else
                {
                    IsLocationAvailable = false;
                }
            }
            finally
            {
                IsLoading = false;
                _isInitializing = false;
            }

            // Weather corre à parte, sem bloquear o botão Start
            if (location is not null)
            {
                _ = LoadWeatherAsync(location);
            }
        }

        [RelayCommand(CanExecute = nameof(CanStartActivity))]
        private async Task StartActivityAsync()
        {
            await Shell.Current.GoToAsync("chronometer");
        }

        private bool CanStartActivity() => IsLocationAvailable;

        private static async Task<bool> AskLocationPermissionAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            return status == PermissionStatus.Granted;
        }

        private static async Task<Microsoft.Maui.Devices.Sensors.Location?> GetCurrentLocationAsync()
        {
            try
            {
                return await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.High,
                        Timeout = TimeSpan.FromSeconds(LocationTimeoutSeconds)
                    });
            }
            catch
            {
                return null;
            }
        }

        private async Task LoadWeatherAsync(Microsoft.Maui.Devices.Sensors.Location location)
        {
            IsLoadingWeather = true;

            try
            {
                var items = await _weatherService.GetForecastAsync(location.Latitude, location.Longitude);
                Forecast = new ObservableCollection<HourlyForecastItem>(items);
            }
            catch
            {
                // TODO: logar falha de weather sem bloquear o ecrã — utilizador ainda pode começar atividade
            }
            finally
            {
                IsLoadingWeather = false;
            }
        }
    }
}