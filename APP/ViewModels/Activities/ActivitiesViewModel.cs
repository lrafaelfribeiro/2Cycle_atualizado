using APP.Components;
using APP.Core;
using APP.Models;
using APP.Services.LocalStorage;
using APP.Services.Session;
using APP.Services.Sharing;
using APP.Services.Sync;
using APP.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace APP.ViewModels
{
    public partial class ActivitiesViewModel : ObservableObject
    {
        private readonly IActivityLocalRepository _repository;
        private readonly IUserContextService _userContextService;
        private readonly IShareCardService _shareCardService;
        private readonly IActivitySyncService _syncService;

        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private ObservableCollection<ActivityListItem> activities = new();

        [ObservableProperty] private int weeklyCount;
        [ObservableProperty] private string weeklyDistanceDisplay = "0 km";
        [ObservableProperty] private string weeklyTimeDisplay = "0h 00m";
        [ObservableProperty] private string weeklyElevationDisplay = "0 m";
        [ObservableProperty] private bool hasNoActivities;

        public ActivitiesViewModel(IActivityLocalRepository repository, IUserContextService userContextService, IShareCardService shareCardService, IActivitySyncService syncService)
        {
            _repository = repository;
            _userContextService = userContextService;
            _shareCardService = shareCardService;
            _syncService = syncService;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var userId = await _userContextService.GetCurrentUserIdAsync();
                if (userId is null) return;

                await _syncService.SyncAsync(); 
                var localActivities = await _repository.GetAllActivitiesAsync(userId);

                var items = new List<ActivityListItem>();
                foreach (var activity in localActivities)
                {
                    items.Add(await ActivityListItemMapper.MapAsync(
                        activity, _repository,
                        openCommand: OpenActivityCommand,
                        optionsCommand: ShowOptionsCommand));
                }

                Activities = new ObservableCollection<ActivityListItem>(items);
                HasNoActivities = Activities.Count == 0;

                CalculateWeeklySummary(localActivities);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ShowOptionsAsync(Guid activityId)
        {
            var choice = await ActionSheetPage.ShowAsync(ActivityActionSheetOptions);

            switch (choice)
            {
                case EditResultKey:
                    await Shell.Current.CurrentPage.DisplayAlertAsync("Em breve", "Editar atividade — ainda não implementado.", "OK");
                    break;

                case ShareResultKey:
                    await ShareActivityAsync(activityId);
                    break;

                case DuplicateResultKey:
                    await Shell.Current.CurrentPage.DisplayAlertAsync("Em breve", "Duplicar atividade — ainda não implementado.", "OK");
                    break;

                case DeleteResultKey:
                    bool confirmed = await Shell.Current.CurrentPage.DisplayAlertAsync(
                        "Eliminar atividade", "Esta ação não pode ser desfeita. Tens a certeza?", "Eliminar", "Cancelar");

                    if (confirmed)
                    {
                        await _repository.MarkAsPendingDeletionAsync(activityId);
                        await LoadAsync();
                    }
                    break;
            }
        }

        // Constantes de resultado — evitam strings mágicas espalhadas entre a definição
        // das opções e o switch que as trata.
        private const string EditResultKey = "edit";
        private const string ShareResultKey = "share";
        private const string DuplicateResultKey = "duplicate";
        private const string DeleteResultKey = "delete";

        // Lista estática porque não depende de estado da instância — evita recriar a
        // mesma lista de ActionSheetOption a cada tap em "⋯".
        private static readonly IReadOnlyList<ActionSheetOption> ActivityActionSheetOptions = new List<ActionSheetOption>
        {
            new() { IconSource = "pencil.svg", Text = "Editar atividade", ResultKey = EditResultKey },
            new() { IconSource = "share.svg", Text = "Partilhar", ResultKey = ShareResultKey },
            new() { IconSource = "duplicate.svg", Text = "Duplicar", ResultKey = DuplicateResultKey },
            new() { IconSource = "trash.svg", Text = "Eliminar atividade", ResultKey = DeleteResultKey, IsDestructive = true },
        };

        private async Task ShareActivityAsync(Guid activityId)
        {
            var item = Activities.FirstOrDefault(a => a.Id == activityId);
            if (item is null) return;

            string imagePath = await _shareCardService.GenerateShareCardAsync(item);

            var page = new ShareCardPreviewPage(imagePath, item.Title);
            await Shell.Current.Navigation.PushModalAsync(page);
        }

        private void CalculateWeeklySummary(List<Models.Local.LocalActivity> localActivities)
        {
            var startOfWeek = DateHelper.GetStartOfWeek(DateTime.UtcNow);

            var thisWeek = localActivities
                .Where(a => a.StartedAt.ToLocalTime().Date >= startOfWeek)
                .ToList();

            WeeklyCount = thisWeek.Count;
            WeeklyDistanceDisplay = $"{thisWeek.Sum(a => a.DistanceMeters) / 1000.0:F1} km";
            WeeklyElevationDisplay = $"{thisWeek.Sum(a => a.ElevationGainMeters):F0} m";

            int totalSeconds = thisWeek.Sum(a => a.TotalTimeSeconds);
            WeeklyTimeDisplay = $"{totalSeconds / 3600}h {(totalSeconds % 3600) / 60:00}m";
        }

        [RelayCommand]
        private static async Task OpenActivityAsync(Guid activityId)
        {
            await Shell.Current.GoToAsync($"activity-summary?activityId={activityId}");
        }

    }
}
