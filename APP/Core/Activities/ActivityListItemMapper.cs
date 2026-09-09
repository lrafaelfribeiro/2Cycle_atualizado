using APP.Models;
using APP.Models.Local;
using APP.Services.LocalStorage;
using Microsoft.Maui.Devices.Sensors;
using System.Globalization;
using System.Windows.Input;

namespace APP.Core
{
    public static class ActivityListItemMapper
    {
        public static async Task<ActivityListItem> MapAsync(
            LocalActivity activity,
            IActivityLocalRepository repository,
            ICommand? openCommand = null,
            ICommand? optionsCommand = null)
        {
            var routePoints = new List<Location>();
            var segments = await repository.GetSegmentsAsync(activity.Id);
            foreach (var segment in segments)
            {
                var points = await repository.GetPointsAsync(segment.Id);
                routePoints.AddRange(points.Select(p => new Location(p.Latitude, p.Longitude)));
            }

            return new ActivityListItem
            {
                Id = activity.Id,
                Title = BuildTitle(activity.StartedAt),
                DateDisplay = FormatDate(activity.StartedAt),
                DistanceDisplay = $"{activity.DistanceMeters / 1000.0:F1} km",
                DurationDisplay = FormatDuration(activity.TotalTimeSeconds),
                ElevationDisplay = $"{activity.ElevationGainMeters:F0} m",
                RoutePoints = routePoints,
                OpenCommand = openCommand,
                OptionsCommand = optionsCommand
            };
        }

        private static string BuildTitle(DateTime startedAt)
        {
            var localDateTime = startedAt.ToLocalTime();

            string dayName = localDateTime.ToString("dddd", new CultureInfo("pt-PT"));
            dayName = char.ToUpper(dayName[0]) + dayName[1..];

            return $"Volta de {dayName}";
        }

        private static string FormatDate(DateTime startedAt)
        {
            var localDateTime = startedAt.ToLocalTime();

            var today = DateTime.Now.Date;
            var date = localDateTime.Date;

            string dayLabel = date == today ? "Hoje"
                : date == today.AddDays(-1) ? "Ontem"
                : localDateTime.ToString("dddd", new CultureInfo("pt-PT"));

            dayLabel = char.ToUpper(dayLabel[0]) + dayLabel[1..];

            return $"{dayLabel} às {localDateTime:HH:mm}";
        }

        private static string FormatDuration(int totalSeconds)
        {
            var span = TimeSpan.FromSeconds(totalSeconds);
            return span.Hours > 0 ? $"{span.Hours}h {span.Minutes:00}m" : $"{span.Minutes}m {span.Seconds:00}s";
        }
    }
}
