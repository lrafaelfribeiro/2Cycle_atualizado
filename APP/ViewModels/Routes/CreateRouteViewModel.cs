using APP.DTOs.Routes.Requests;
using APP.DTOs.Routes.Responses;
using APP.Services.Routes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace APP.ViewModels
{
    public partial class CreateRouteViewModel : ObservableObject
    {
        private readonly IRouteService _routeService;
        private const string HintDismissedKey = "CreateRouteHintDismissed";

        public event Action<List<RoutePointResponse>>? DrawRouteRequested;
        public event Action<bool>? ModeChanged;
        public event Action<double, double, double, double>? PointsSwapped; // originLat, originLng, destLat, destLng
        public event Action<double, double>? LocationRequested; // lat, lng
        public event Action<bool>? LockChanged;      // novo
        public event Action? MapClearRequested;

        public CreateRouteViewModel(IRouteService routeService)
        {
            _routeService = routeService;
        }

        [ObservableProperty] private bool isRoundTrip;
        [ObservableProperty] private double? originLatitude;
        [ObservableProperty] private double? originLongitude;
        [ObservableProperty] private double? destinationLatitude;
        [ObservableProperty] private double? destinationLongitude;
        [ObservableProperty] private double desiredDistanceKm = 10;
        [ObservableProperty] private string routeName = string.Empty;
        [ObservableProperty] private bool hasCalculatedRoute;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool showHint = !Preferences.Default.Get(HintDismissedKey, false);
        [ObservableProperty] private bool isSheetExpanded = true;


        public bool CanEditPoints => !HasCalculatedRoute;
        private Guid? _lastSuggestedRouteId;
        private double _distanceMeters;
        private double _elevationGainMeters;

        public string DistanceLabel => $"{_distanceMeters / 1000:F2} km";
        public string ElevationLabel => $"{_elevationGainMeters:F0} m";

        public bool CanCalculate => IsRoundTrip
            ? OriginLatitude.HasValue
            : OriginLatitude.HasValue && DestinationLatitude.HasValue;

        public bool CanSwap => !IsRoundTrip && OriginLatitude.HasValue && DestinationLatitude.HasValue;

        public string CalculateButtonLabel => HasCalculatedRoute ? "Recalcular rota" : "Calcular rota";

        partial void OnHasCalculatedRouteChanged(bool value) => OnPropertyChanged(nameof(CalculateButtonLabel));

        [RelayCommand]
        private void ToggleSheet() => IsSheetExpanded = !IsSheetExpanded;

        [RelayCommand]
        private void DismissHint()
        {
            ShowHint = false;
            Preferences.Default.Set(HintDismissedKey, true);
        }

        [RelayCommand]
        private void SetPointToPointMode()
        {
            IsRoundTrip = false;
            ResetPoints();
            ModeChanged?.Invoke(false);
        }

        [RelayCommand]
        private void SetRoundTripMode()
        {
            IsRoundTrip = true;
            ResetPoints();
            ModeChanged?.Invoke(true);
        }

        [RelayCommand]
        private void SwapPoints()
        {
            if (!CanSwap) return;

            (OriginLatitude, DestinationLatitude) = (DestinationLatitude, OriginLatitude);
            (OriginLongitude, DestinationLongitude) = (DestinationLongitude, OriginLongitude);

            PointsSwapped?.Invoke(OriginLatitude!.Value, OriginLongitude!.Value, DestinationLatitude!.Value, DestinationLongitude!.Value);
            HasCalculatedRoute = false;
        }

        [RelayCommand]
        private async Task LocateMeAsync()
        {
            try
            {
                var location = await Geolocation.Default.GetLastKnownLocationAsync()
                    ?? await Geolocation.Default.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));

                if (location != null)
                {
                    LocationRequested?.Invoke(location.Latitude, location.Longitude);
                }
            }
            catch
            {
                await Shell.Current.DisplayAlertAsync("Localização", $"Não foi possível obter a localização", "OK");
            }
        }

        private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public void OnMapTapped(string rawMessage)
        {
            try
            {
                var msg = System.Text.Json.JsonSerializer.Deserialize<MapTapMessage>(rawMessage, JsonOptions)!;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (msg.Type == "origin")
                    {
                        OriginLatitude = msg.Lat;
                        OriginLongitude = msg.Lng;
                    }
                    else if (msg.Type == "destination")
                    {
                        DestinationLatitude = msg.Lat;
                        DestinationLongitude = msg.Lng;
                    }

                    HasCalculatedRoute = false;
                    OnPropertyChanged(nameof(CanCalculate));
                    OnPropertyChanged(nameof(CanSwap));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OnMapTapped] Falhou a processar mensagem do mapa: {ex}");
            }
        }

        [RelayCommand]
        private async Task CalculateRouteAsync()
        {
            IsBusy = true;
            try
            {
                RouteSuggestionResponse response = IsRoundTrip
                    ? await _routeService.SuggestRoundTripAsync(new RouteRoundTripSuggestionRequest(
                        OriginLatitude!.Value, OriginLongitude!.Value, DesiredDistanceKm * 1000))
                    : await _routeService.SuggestAsync(new RouteSuggestionRequest(
                        OriginLatitude!.Value, OriginLongitude!.Value,
                        DestinationLatitude!.Value, DestinationLongitude!.Value));

                _lastSuggestedRouteId = response.SuggestedRouteId;
                _distanceMeters = response.DistanceMeters;
                _elevationGainMeters = response.ElevationGainMeters;
                HasCalculatedRoute = true;

                OnPropertyChanged(nameof(DistanceLabel));
                OnPropertyChanged(nameof(ElevationLabel));
                DrawRouteRequested?.Invoke(response.Points);
                LockChanged?.Invoke(true);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Erro", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void RecalculateRoute()
        {
            ResetPoints();
            _lastSuggestedRouteId = null;

            OnPropertyChanged(nameof(CanCalculate));
            OnPropertyChanged(nameof(CanSwap));
            OnPropertyChanged(nameof(CanEditPoints));

            MapClearRequested?.Invoke(); // limpa marcadores + linha no mapa
            LockChanged?.Invoke(false);  // desbloqueia para poderes tocar em novos pontos
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (_lastSuggestedRouteId is null || IsBusy) return;

            IsBusy = true;
            try
            {
                await _routeService.SaveAsync(_lastSuggestedRouteId.Value, RouteName);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Não foi possível guardar", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");

        private void ResetPoints()
        {
            OriginLatitude = OriginLongitude = DestinationLatitude = DestinationLongitude = null;
            HasCalculatedRoute = false;
        }

        private record MapTapMessage(string Type, double Lat, double Lng);
    }
}