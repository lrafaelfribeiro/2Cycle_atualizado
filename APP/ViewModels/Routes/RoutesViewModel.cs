using APP.Components;
using APP.Core;
using APP.Core.Messages;
using APP.DTOs.Routes;
using APP.Models;
using APP.Models.Routes;
using APP.Services.Routes;
using APP.Services.Thumbnails;
using APP.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using static APP.Models.Routes.RouteListItem;

namespace APP.ViewModels
{
    public partial class RoutesViewModel : ObservableObject
    {
        private readonly IRouteService _routeService;
        private readonly IRouteThumbnailService _thumbnailService;
        private readonly IRouteSaveCoordinator _routeSaveCoordinator;
        private List<RouteListItem> _allRoutes = new();
        private bool _hasLoadedOnce;

        private const string RenameResultKey = "rename";
        private const string DeleteResultKey = "delete";

        private static readonly IReadOnlyList<ActionSheetOption> RouteActionSheetOptions = new List<ActionSheetOption>
        {
            new() { IconSource = "pencil.svg", Text = "Editar nome", ResultKey = RenameResultKey },
            new() { IconSource = "trash.svg", Text = "Eliminar rota", ResultKey = DeleteResultKey, IsDestructive = true },
        };


        [ObservableProperty]
        private ObservableCollection<RouteListItem> displayedRoutes = new();

        [ObservableProperty]
        private bool showingFavoritesOnly = true;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isInitialLoading;
        public RoutesViewModel(IRouteService routeService, IRouteThumbnailService thumbnailService, IRouteSaveCoordinator routeSaveCoordinator)
        {
            _routeService = routeService;
            _thumbnailService = thumbnailService;
            _routeSaveCoordinator = routeSaveCoordinator;

            RegisterSaveMessages();
        }

        private void RegisterSaveMessages()
        {
            // Guarda contra registo duplicado — se a VM for reaproveitada
            // (cache de tab do MainTabPage), o construtor só corre uma vez,
            // mas mantém a verificação por segurança/robustez.
            if (WeakReferenceMessenger.Default.IsRegistered<RouteSaveStartedMessage>(this))
            {
                return;
            }

            WeakReferenceMessenger.Default.Register<RouteSaveStartedMessage>(this, (r, m) =>
                ((RoutesViewModel)r).OnRouteSaveStarted(m));

            WeakReferenceMessenger.Default.Register<RouteSaveCompletedMessage>(this, (r, m) =>
                ((RoutesViewModel)r).OnRouteSaveCompleted(m));

            WeakReferenceMessenger.Default.Register<RouteSaveFailedMessage>(this, (r, m) =>
                ((RoutesViewModel)r).OnRouteSaveFailed(m));
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            // Primeira carga: overlay dedicado, sem tocar no RefreshView
            // Cargas seguintes (pull-to-refresh): spinner nativo do RefreshView
            bool isFirstLoad = !_hasLoadedOnce;

            if (isFirstLoad)
            {
                IsInitialLoading = true;
            }
            else
            {
                IsBusy = true;
            }

            try
            {
                await LoadRoutesInternalAsync();
                _hasLoadedOnce = true;
            }
            finally
            {
                IsInitialLoading = false;
                IsBusy = false;
            }
        }

        // Reload interno, sem tocar em IsBusy — usado quando já há outro
        // feedback visual a cobrir a espera (ex: skeleton do save em progresso)
        private async Task ReloadSilentlyAsync()
        {
            await LoadRoutesInternalAsync();
        }

        private async Task LoadRoutesInternalAsync()
        {
            var routes = await _routeService.GetSavedAsync(favoritesOnly: false);

            var withThumbnails = await Task.WhenAll(routes.Select(async r =>
            {
                var points = r.PreviewPoints
                    .Select(p => (p.Latitude, p.Longitude))
                    .ToList();

                string? path = await _thumbnailService.GetOrCreateThumbnailPathAsync(
                    r.SuggestedRouteId, points);

                return r with { ThumbnailPath = path };
            }));

            _allRoutes = withThumbnails
                .Select(r => RouteListItemMapper.FromResponse(
                    r, OpenCommand, OptionsCommand, ToggleFavoriteCommand))
                .ToList();

            InsertPendingPlaceholders();
            ApplyFilter();
        }


        // Cobre saves que arrancaram noutra página e ainda não terminaram
        // quando esta lista é (re)carregada.
        private void InsertPendingPlaceholders()
        {
            foreach (var pending in _routeSaveCoordinator.GetPendingSaves())
            {
                if (_allRoutes.Any(r => r.PendingSaveId == pending.PendingSaveId))
                {
                    continue; // já lá está, inserido via OnRouteSaveStarted
                }

                _allRoutes.Insert(0, RouteListItem.CreatePlaceholder(pending.PendingSaveId, pending.RouteName));
            }
        }

        private void OnRouteSaveStarted(RouteSaveStartedMessage message)
        {
            // Evita duplicar se, por alguma razão, InsertPendingPlaceholders (no LoadAsync)
            // já tiver inserido este mesmo save entretanto
            if (_allRoutes.Any(r => r.PendingSaveId == message.PendingSaveId))
            {
                return;
            }

            _allRoutes.Insert(0, RouteListItem.CreatePlaceholder(message.PendingSaveId, message.RouteName));
            ApplyFilter();
        }

        private async void OnRouteSaveCompleted(RouteSaveCompletedMessage message)
        {
            // A fonte da verdade é o backend — recarregar é mais simples e correto
            // do que tentar reconstruir manualmente o item (evita duplicar a
            // lógica de thumbnail/mapper que já vive em LoadAsync).
            try
            {
                await ReloadSilentlyAsync();
            }
            catch (Exception ex)
            {
                // logging / toast de erro, conforme o resto do projeto
            }
        }

        private void OnRouteSaveFailed(RouteSaveFailedMessage message)
        {
            var placeholder = _allRoutes.FirstOrDefault(r => r.PendingSaveId == message.PendingSaveId);
            if (placeholder is null)
            {
                return;
            }

            placeholder.State = RouteListItemState.Failed;
            placeholder.SaveErrorMessage = message.ErrorMessage;
            ApplyFilter();
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