using APP.Core;
using APP.Models;
using APP.Services.LocalStorage;
using APP.Services.Session;
using APP.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace APP.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly IUserContextService _userContextService;
        private readonly IActivityLocalRepository _repository;

        [ObservableProperty]
        private WeeklyStatsDTO? weeklyStats;

        [ObservableProperty]
        private bool isLoadingStats;

        [ObservableProperty]
        private string greetingName = string.Empty;

        public HomeViewModel(IUserContextService userContextService, IActivityLocalRepository repository)
        {
            _userContextService = userContextService;
            _repository = repository;
        }

        [RelayCommand]
        public async Task LoadGreetingAsync()
        {
            GreetingName = await _userContextService.GetCurrentUserNameAsync() ?? "";
        }

        [RelayCommand]
        public async Task RefreshWeeklyStatisticsAsync()
        {
            IsLoadingStats = true;
            try
            {
                var userId = await _userContextService.GetCurrentUserIdAsync();
                if (userId is null)
                {
                    WeeklyStats = new WeeklyStatsDTO(0, TimeSpan.Zero, 0);
                    return;
                }

                var activities = await _repository.GetAllActivitiesAsync(userId);

                var startOfWeek = DateHelper.GetStartOfWeek(DateTime.UtcNow);
                var thisWeek = activities.Where(a => a.StartedAt.Date >= startOfWeek).ToList();

                WeeklyStats = new WeeklyStatsDTO(
                    thisWeek.Sum(a => a.DistanceMeters) / 1000.0,
                    TimeSpan.FromSeconds(thisWeek.Sum(a => a.MovingTimeSeconds)),
                    thisWeek.Sum(a => a.ElevationGainMeters)
                );
            }
            finally
            {
                IsLoadingStats = false;
            }
        }

        [RelayCommand]
        private async Task NavigateToRoutesAsync()
        {
            await Shell.Current.GoToAsync("//routes");
        }
    }
}
