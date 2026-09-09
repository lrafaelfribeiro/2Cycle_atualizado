using APP.Core;
using APP.Models.Local;
using APP.Services.LocalStorage;
using APP.Services.Sync;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace APP.ViewModels
{
    public partial class ActivitySummaryViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IActivityLocalRepository _repository;
        private Guid _activityId;

        [ObservableProperty] private bool isLoading = true;
        [ObservableProperty] private string distanceDisplay = "--";
        [ObservableProperty] private string totalTimeDisplay = "--";
        [ObservableProperty] private string paceDisplay = "--";
        [ObservableProperty] private string avgSpeedDisplay = "--";
        [ObservableProperty] private string elevationDisplay = "--";
        [ObservableProperty] private ObservableCollection<SpeedZoneResult> zones = new();

        public event EventHandler? DataLoaded;
        public List<Location> RoutePoints { get; private set; } = new();
        public List<SpeedSample> SpeedSamples { get; private set; } = new();

        private readonly IActivitySyncService _syncService;

        public ActivitySummaryViewModel(IActivityLocalRepository repository, IActivitySyncService syncService)
        {
            _repository = repository;
            _syncService = syncService;
        }
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("activityId", out var value) && Guid.TryParse(value.ToString(), out var id))
            {
                _activityId = id;
                _ = LoadAsync();
            }
        }

        private async Task LoadAsync()
        {
            IsLoading = true;

            var activity = await _repository.GetActivityByIdAsync(_activityId);
            if (activity is null)
            {
                IsLoading = false;
                return;
            }

            DistanceDisplay = $"{activity.DistanceMeters / 1000.0:F1}";
            TotalTimeDisplay = TimeSpan.FromSeconds(activity.TotalTimeSeconds).ToString(@"hh\:mm\:ss");
            AvgSpeedDisplay = $"{activity.AverageSpeedKmh:F1}";
            ElevationDisplay = $"{activity.ElevationGainMeters:F0}";

            double distanceKm = activity.DistanceMeters / 1000.0;
            PaceDisplay = distanceKm > 0.05
                ? FormatPace(activity.MovingTimeSeconds / 60.0 / distanceKm)
                : "--:--";

            // Descarregar os pontos do gps se veio de outro dispositivo
            await _syncService.EnsureTrackDownloadedAsync(_activityId);

            var segments = await _repository.GetSegmentsAsync(_activityId);
            var segmentData = new List<(List<LocalTrackPoint> Points, DateTime SegmentStart)>();

            var routePoints = new List<Location>();
            var speedSamples = new List<SpeedSample>();
            double cumulativeKm = 0;

            foreach (var segment in segments)
            {
                var points = await _repository.GetPointsAsync(segment.Id);
                segmentData.Add((points, segment.StartedAt));

                for (int i = 0; i < points.Count; i++)
                {
                    routePoints.Add(new Location(points[i].Latitude, points[i].Longitude));
                    if (i == 0) continue; // primeiro ponto do segmento não liga ao segmento anterior (pausa)

                    var prev = points[i - 1];
                    var curr = points[i];
                    double deltaSeconds = (curr.RecordedAt - prev.RecordedAt).TotalSeconds;
                    if (deltaSeconds <= 0) continue;

                    double deltaMeters = GeoMath.HaversineDistanceMeters(prev.Latitude, prev.Longitude, curr.Latitude, curr.Longitude);
                    double speedKmh = (deltaMeters / deltaSeconds) * 3.6;
                    if (speedKmh > 180) continue; // mesmo filtro de ruído do resto do app

                    cumulativeKm += deltaMeters / 1000.0;
                    speedSamples.Add(new SpeedSample(cumulativeKm, speedKmh));
                }
            }

            RoutePoints = routePoints;
            SpeedSamples = speedSamples;

            var zoneResults = SpeedZoneCalculator.Calculate(segmentData);
            Zones = new ObservableCollection<SpeedZoneResult>(zoneResults);

            IsLoading = false;
            DataLoaded?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            _ = _syncService.SyncPendingActivitiesAsync(); // fire-and-forget — não bloqueia navegação com rede lenta/em falta

            await Shell.Current.GoToAsync("//home");
        }

        private static string FormatPace(double minutesPerKm)
        {
            int minutes = (int)minutesPerKm;
            int seconds = (int)Math.Round((minutesPerKm - minutes) * 60);
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
