using APP.DTOs.Routes;
using APP.DTOs.Routes.Responses;
using APP.Models;
using APP.Services.Routes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace APP.ViewModels
{
    public partial class RouteDetailViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IRouteService _routeService;
        private Guid _suggestedRouteId;

        [ObservableProperty] private bool isLoading = true;
        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private string distanceDisplay = "--";
        [ObservableProperty] private string elevationGainDisplay = "--";
        [ObservableProperty] private string netElevationDisplay = "--";
        [ObservableProperty] private bool isFavorite;
        [ObservableProperty] private RouteDifficulty difficulty;
        [ObservableProperty] private bool isProDifficulty;

        public event EventHandler? DataLoaded;
        public List<(double Latitude, double Longitude)> RoutePoints { get; private set; } = new();
        public List<ElevationPointResponse> ElevationProfile { get; private set; } = new();

        public RouteDetailViewModel(IRouteService routeService)
        {
            _routeService = routeService;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("routeId", out var value) && Guid.TryParse(value.ToString(), out var id))
            {
                _suggestedRouteId = id;
                _ = LoadAsync();
            }
        }

        private async Task LoadAsync()
        {
            IsLoading = true;

            try
            {
                // Esta página mostra Nome/Favorito, por isso precisa do detalhe da rota GUARDADA
                // (não do preview genérico, que não tem essa informação).
                var detail = await _routeService.GetSavedRouteDetailAsync(_suggestedRouteId);

                Name = detail.Name ?? string.Empty;
                DistanceDisplay = $"{detail.DistanceMeters / 1000.0:F2}";
                ElevationGainDisplay = $"{detail.ElevationGainMeters:F0}";
                NetElevationDisplay = $"{detail.ElevationNetMeters:F0}";
                IsFavorite = detail.IsFavorite;
                Difficulty = detail.Difficulty;
                IsProDifficulty = detail.Difficulty == RouteDifficulty.Pro;

                RoutePoints = detail.Polyline.Select(p => (p.Latitude, p.Longitude)).ToList();
                ElevationProfile = detail.ElevationProfile;

                DataLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (ApplicationException)
            {
                // TODO: mesmo padrão de erro que usas noutras pages — Snackbar? Alert?
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ToggleFavoriteAsync()
        {
            bool newValue = !IsFavorite;

            try
            {
                await _routeService.SetFavoriteAsync(_suggestedRouteId, newValue);
                IsFavorite = newValue;
            }
            catch (ApplicationException)
            {
                // Falhou no servidor — não mudamos o estado local, fica como estava.
                // TODO: mesmo padrão de erro que usas noutras pages.
            }
        }
    }
}