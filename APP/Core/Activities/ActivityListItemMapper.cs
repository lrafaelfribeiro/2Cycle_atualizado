using APP.Models;
using APP.Models.Local;
using APP.Services.LocalStorage;
using APP.Services.Thumbnails;
using Microsoft.Maui.Devices.Sensors;
using System.Globalization;
using System.Windows.Input;

namespace APP.Core
{
    public static class ActivityListItemMapper
    {
        // Maior que o tamanho de display (90x90dp) para manter nitidez em ecrãs
        // de alta densidade — é gerado uma única vez e cacheado, o custo extra é irrelevante.
        private const int ThumbnailSizePx = 180;

        public static async Task<ActivityListItem> MapAsync(
            LocalActivity activity,
            IActivityLocalRepository repository,
            IRouteThumbnailService thumbnailService,
            ICommand? openCommand = null,
            ICommand? optionsCommand = null)
        {
            var routePoints = await LoadRoutePointsAsync(activity.Id, repository);
            string? thumbnailPath = await TryGetThumbnailAsync(activity.Id, routePoints, thumbnailService);

            return new ActivityListItem
            {
                Id = activity.Id,
                Title = BuildTitle(activity.StartedAt),
                DateDisplay = FormatDate(activity.StartedAt),
                DistanceDisplay = $"{activity.DistanceMeters / 1000.0:F1} km",
                DurationDisplay = FormatDuration(activity.TotalTimeSeconds),
                ElevationDisplay = $"{activity.ElevationGainMeters:F0} m",
                ThumbnailPath = thumbnailPath,
                OpenCommand = openCommand,
                OptionsCommand = optionsCommand
            };
        }

        private static async Task<List<Location>> LoadRoutePointsAsync(
            Guid activityId, IActivityLocalRepository repository)
        {
            var routePoints = new List<Location>();
            var segments = await repository.GetSegmentsAsync(activityId);

            foreach (var segment in segments)
            {
                var points = await repository.GetPointsAsync(segment.Id);
                routePoints.AddRange(points.Select(p => new Location(p.Latitude, p.Longitude)));
            }

            return routePoints;
        }

        private static Task<string?> TryGetThumbnailAsync(
            Guid activityId, List<Location> routePoints, IRouteThumbnailService thumbnailService)
        {
            if (routePoints.Count < 2)
                return Task.FromResult<string?>(null);

            var latLonPoints = routePoints.Select(p => (p.Latitude, p.Longitude)).ToList();

            return thumbnailService.GetOrCreateThumbnailPathAsync(
                activityId, latLonPoints, widthPx: ThumbnailSizePx, heightPx: ThumbnailSizePx);
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