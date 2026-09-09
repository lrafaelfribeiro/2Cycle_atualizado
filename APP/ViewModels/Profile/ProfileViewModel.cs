using APP.Services.LocalStorage;
using APP.Services.Session;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace APP.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly IActivityLocalRepository _repository;
        private readonly IUserContextService _userContextService;

        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private string username = string.Empty;
        [ObservableProperty] private string cyclingSinceDisplay = string.Empty;

        [ObservableProperty] private string totalDistanceDisplay = "0 km";
        [ObservableProperty] private string totalTimeDisplay = "0h 00m";
        [ObservableProperty] private string totalElevationDisplay = "0 m";
        [ObservableProperty] private int totalActivities;

        public ProfileViewModel(IActivityLocalRepository repository, IUserContextService userContextService)
        {
            _repository = repository;
            _userContextService = userContextService;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                Username = await _userContextService.GetCurrentUserNameAsync() ?? "Ciclista";

                var userId = await _userContextService.GetCurrentUserIdAsync();
                if (userId is null) return;

                var activities = await _repository.GetAllActivitiesAsync(userId);

                TotalActivities = activities.Count;
                TotalDistanceDisplay = $"{activities.Sum(a => a.DistanceMeters) / 1000.0:F1} km";
                TotalElevationDisplay = $"{activities.Sum(a => a.ElevationGainMeters):F0} m";

                int totalSeconds = activities.Sum(a => a.TotalTimeSeconds);
                TotalTimeDisplay = $"{totalSeconds / 3600}h {(totalSeconds % 3600) / 60:00}m";

                CyclingSinceDisplay = activities.Count > 0
                    ? $"Ciclista desde {activities.Min(a => a.StartedAt).Year}"
                    : "";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
