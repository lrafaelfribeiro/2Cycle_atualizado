using APP.Components;
using APP.DTOs.Routes;
using APP.Mappers;
using APP.Models;
using APP.Services.Routes;
using APP.Services.Thumbnails;
using APP.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace APP.ViewModels
{
    public partial class RoutesViewModel : ObservableObject
    {
        private readonly IRouteService _routeService;
        private readonly IRouteThumbnailService _thumbnailService;
        private List<RouteListItem> _allRoutes = new();

        private const string RenameResultKey = "rename";
        private const string DeleteResultKey = "delete";

        private static readonly IReadOnlyList<ActionSheetOption> RouteActionSheetOptions = new List<ActionSheetOption>
        {
            new() { IconSource = "pencil.svg", Text = "Editar nome", ResultKey = RenameResultKey },
            new() { IconSource = "trash.svg", Text = "Eliminar rota", ResultKey = DeleteResultKey, IsDestructive = true },
        };

        public RoutesViewModel(IRouteService routeService, IRouteThumbnailService thumbnailService)
        {
            _routeService = routeService;
            _thumbnailService = thumbnailService;
        }

        [ObservableProperty]
        private ObservableCollection<RouteListItem> displayedRoutes = new();

        [ObservableProperty]
        private bool showingFavoritesOnly = true;

        [ObservableProperty]
        private bool isBusy;
        [ObservableProperty] private bool isInitialLoading;

        [RelayCommand]
        private async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            bool isFirstLoad = DisplayedRoutes.Count == 0;
            if (isFirstLoad) IsInitialLoading = true;

            try
            {
                var routes = await _routeService.GetSavedAsync(favoritesOnly: false);

                var withThumbnails = await Task.WhenAll(routes.Select(async r =>
                {
                    var points = r.PreviewPoints
                        .Select(p => (p.Latitude, p.Longitude))
                        .ToList();

                    string? path = await _thumbnailService.GetOrCreateThumbnailPathAsync(
                        r.SuggestedRouteId, points);

                    return r with { ThumbnailPath = path }; // OK aqui — SavedRouteSummaryResponse É record
                }));

                _allRoutes = withThumbnails
                    .Select(r => RouteListItemMapper.FromResponse(
                        r,
                        OpenCommand,
                        OptionsCommand,
                        ToggleFavoriteCommand))
                    .ToList();

                ApplyFilter();
            }
            finally
            {
                IsBusy = false;
                IsInitialLoading = false;
            }
        }

        [RelayCommand]
        private void ShowFavorites()
        {
            ShowingFavoritesOnly = true;
            ApplyFilter();
        }

        [RelayCommand]
        private void ShowAll()
        {
            ShowingFavoritesOnly = false;
            ApplyFilter();
        }

        [RelayCommand]
        private async Task ToggleFavoriteAsync(RouteListItem route)
        {
            bool newValue = !route.IsFavorite;
            await _routeService.SetFavoriteAsync(route.SuggestedRouteId, newValue);

            // Mutação direta: IsFavorite é [ObservableProperty], a UI do próprio
            // card atualiza sozinha (estrela). Mas se estiveres a ver só
            // favoritos e desmarcaste um, o item tem de DESAPARECER da lista
            // visível — isso não é o card a reagir, é preciso re-filtrar.
            route.IsFavorite = newValue;
            ApplyFilter();
        }

        [RelayCommand]
        private static async Task CreateRouteAsync()
        {
            await Shell.Current.GoToAsync("create-route");
        }

        [RelayCommand]
        private static async Task OpenAsync(RouteListItem route)
        {
            await Shell.Current.GoToAsync($"routedetail?routeId={route.SuggestedRouteId}");
        }

        [RelayCommand]
        private async Task OptionsAsync(RouteListItem route)
        {
            var choice = await ActionSheetPage.ShowAsync(RouteActionSheetOptions);

            switch (choice)
            {
                case RenameResultKey:
                    await RenameRouteAsync(route);
                    break;
                case DeleteResultKey:
                    await DeleteRouteAsync(route);
                    break;
            }
        }

        private async Task RenameRouteAsync(RouteListItem route)
        {
            string? newName = await Shell.Current.CurrentPage.DisplayPromptAsync(
                title: "Editar nome",
                message: "Novo nome da rota",
                accept: "Guardar",
                cancel: "Cancelar",
                initialValue: route.Title,
                maxLength: 100);

            if (string.IsNullOrWhiteSpace(newName) || newName == route.Title)
            {
                return;
            }

            await _routeService.RenameAsync(route.SuggestedRouteId, newName);

            // Title não é [ObservableProperty] (não muda por si em runtime, só via
            // esta ação explícita) — a mutação sozinha não repinta o card. Por
            // isso ApplyFilter() é necessário aqui: reconstrói a ObservableCollection,
            // o que força a CollectionView a reler o binding de Title com o valor novo.
            route.Title = newName;
            ApplyFilter();
        }

        private async Task DeleteRouteAsync(RouteListItem route)
        {
            var currentPage = Application.Current!.Windows[0].Page!;

            bool confirmed = await currentPage.DisplayAlertAsync(
                "Eliminar rota",
                $"Tens a certeza que queres eliminar \"{route.Title}\"?",
                "Eliminar",
                "Cancelar");

            if (!confirmed)
            {
                return;
            }

            await _routeService.UnsaveAsync(route.SuggestedRouteId);

            _allRoutes.RemoveAll(r => r.SuggestedRouteId == route.SuggestedRouteId);
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var filtered = ShowingFavoritesOnly
                ? _allRoutes.Where(r => r.IsFavorite)
                : _allRoutes;

            DisplayedRoutes = new ObservableCollection<RouteListItem>(filtered);
        }
    }
}