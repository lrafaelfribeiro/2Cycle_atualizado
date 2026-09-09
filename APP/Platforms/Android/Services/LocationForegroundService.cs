using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using APP.Core;
using APP.Models.Local;
using APP.Services.LocalStorage;
using APP.Services.LocationTracking;
using CommunityToolkit.Mvvm.Messaging;

namespace APP.Platforms.Android.Services
{
    [Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation, Exported = false)]
    public class LocationForegroundService : Service, ILocationListener
    {
        public const string ActionStart = "com.twocycle.app.action.START";
        public const string ActionPause = "com.twocycle.app.action.PAUSE";
        public const string ActionResume = "com.twocycle.app.action.RESUME";
        public const string ActionStop = "com.twocycle.app.action.STOP";
        public const string ActionSetAutoPause = "com.twocycle.app.action.SET_AUTO_PAUSE";

        public const string ExtraActivityId = "activity_id";
        public const string ExtraOwnerUserId = "owner_user_id";
        public const string ExtraAutoPauseEnabled = "auto_pause_enabled";

        // --- Auto-pausa ---
        private const double AutoPauseSpeedThresholdKmh = 2.0;   // abaixo disto conta como parado
        private const double AutoResumeSpeedThresholdKmh = 2.5;  // acima disto conta como a mexer (histerese evita oscilar)
        private const int AutoPauseAfterSeconds = 8;             // tempo parado antes de pausar automaticamente

        // --- Qualidade / filtragem do sinal GPS ---
        private const float MaxAcceptableAccuracyMeters = 20f;       // fixes piores que isto são descartados de raiz (indoor/multipath)
        private const double MinMovementThresholdMeters = 4.0;       // fallback (sem Doppler): ruído típico de GPS parado
        private const double MinMovingSpeedKmh = 1.0;                // abaixo disto, mesmo com Doppler, conta como parado
        private const double MinElevationDeltaMeters = 1.5;          // filtra ruído de altitude do GPS/barómetro
        private const double MaxRealisticInstantSpeedMps = 50.0;     // ~180 km/h — corta saltos GPS irrealistas
        private const double MaxPlausibleSpeedKmh = 120.0;           // máx. de velocidade por causa do efeito doppler
        private const double MaxRealisticGradientPercent = 25.0;     // ~25% é já um alpe extremo; acima disto o fix é lixo, não é ciclismo real

        private const string ChannelId = "tracking_channel";
        private const int NotificationId = 1001;

        private bool _autoPauseEnabled;
        private bool _isAutoPaused;
        private int _stationarySeconds;

        private LocationManager? _locationManager;
        private IActivityLocalRepository? _repository;

        private Guid _activityId;
        private string _ownerUserId = string.Empty;
        private Guid _currentSegmentId;
        private int _segmentSequence;
        private int _pointSequence;

        private global::Android.Locations.Location? _lastPoint;
        private double _totalDistanceMeters;
        private double _maxSpeedKmh;
        private DateTime _segmentStartedAt;
        private DateTime _activityStartedAt;
        private int _accumulatedMovingSeconds;
        private DateTime _currentSegmentStartedAt;
        private bool _isPaused;
        private double? _lastAltitude;
        private double _elevationGainMeters;
        private float _lastAccuracy;

        public override IBinder? OnBind(Intent? intent) => null;

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            _repository ??= IPlatformApplication.Current!.Services.GetRequiredService<IActivityLocalRepository>();
            _locationManager ??= (LocationManager)GetSystemService(LocationService)!;

            switch (intent?.Action)
            {
                case ActionStart:
                    _activityId = Guid.Parse(intent.GetStringExtra(ExtraActivityId)!);
                    _ownerUserId = intent.GetStringExtra(ExtraOwnerUserId)!;
                    _autoPauseEnabled = intent.GetBooleanExtra(ExtraAutoPauseEnabled, false);
                    _isAutoPaused = false;
                    _stationarySeconds = 0;
                    _ = HandleStartAsync();
                    break;

                case ActionPause:
                    _ = HandlePauseAsync();
                    break;

                case ActionResume:
                    _ = HandleResumeAsync();
                    break;

                case ActionStop:
                    _ = HandleStopAsync();
                    break;

                case ActionSetAutoPause:
                    _autoPauseEnabled = intent.GetBooleanExtra(ExtraAutoPauseEnabled, false);
                    if (!_autoPauseEnabled) _isAutoPaused = false; // se desligar a meio de uma auto-pausa, retoma logo
                    break;
            }

            return StartCommandResult.Sticky;
        }

        private async Task HandleStartAsync()
        {
            _activityStartedAt = DateTime.UtcNow;
            _segmentStartedAt = _activityStartedAt;
            _segmentSequence = 0;
            _pointSequence = 0;
            _totalDistanceMeters = 0;
            _maxSpeedKmh = 0;
            _accumulatedMovingSeconds = 0;
            _currentSegmentStartedAt = _activityStartedAt;
            _isPaused = false;
            _elevationGainMeters = 0;
            _lastAltitude = null;
            _currentSegmentId = Guid.NewGuid();

            var activity = new LocalActivity
            {
                Id = _activityId,
                OwnerUserId = _ownerUserId,
                StartedAt = _activityStartedAt,
                SyncStatus = SyncStatus.Pending
            };

            var segment = new LocalTrackSegment
            {
                Id = _currentSegmentId,
                LocalActivityId = _activityId,
                SequenceIndex = _segmentSequence,
                StartedAt = _segmentStartedAt
            };

            await _repository!.SaveActivityAsync(activity, new List<LocalTrackSegment> { segment }, new List<LocalTrackPoint>());

            StartForeground(NotificationId, BuildNotification("A gravar percurso..."));
            RequestLocationUpdates();
        }

        private async Task HandlePauseAsync()
        {
            _locationManager?.RemoveUpdates(this);
            await CloseCurrentSegmentAsync();
            _isPaused = true;
            UpdateNotification("Em pausa");
        }

        private async Task HandleResumeAsync()
        {
            _isPaused = false;
            await OpenNewSegmentAsync();
            UpdateNotification("A gravar percurso...");
            RequestLocationUpdates();
        }

        private async Task HandleStopAsync()
        {
            _locationManager?.RemoveUpdates(this);
            await CloseCurrentSegmentAsync();

            var totalTimeSeconds = (int)(DateTime.UtcNow - _activityStartedAt).TotalSeconds;
            var avgSpeedKmh = totalTimeSeconds > 0 ? (_totalDistanceMeters / 1000.0) / (totalTimeSeconds / 3600.0) : 0;

            var db = await IPlatformApplication.Current!.Services.GetRequiredService<APP.Services.LocalStorage.ILocalDatabaseService>().GetConnectionAsync();

            var activity = await db.Table<LocalActivity>().Where(a => a.Id == _activityId).FirstOrDefaultAsync();
            if (activity is not null)
            {
                activity.EndedAt = DateTime.UtcNow;
                activity.DistanceMeters = _totalDistanceMeters;
                activity.TotalTimeSeconds = totalTimeSeconds;
                activity.MovingTimeSeconds = _accumulatedMovingSeconds;
                activity.AverageSpeedKmh = avgSpeedKmh;
                activity.MaxSpeedKmh = _maxSpeedKmh;
                activity.ElevationGainMeters = _elevationGainMeters; // BUG anterior: este valor era calculado mas nunca persistido
                await db.UpdateAsync(activity);
            }

            WeakReferenceMessenger.Default.Send(new LocationTrackingStoppedMessage(_activityId, true));

            if (Build.VERSION.SdkInt >= BuildVersionCodes.N) // API 24+
            {
                StopForeground(StopForegroundFlags.Remove);
            }
            else
            {
#pragma warning disable CS0618
                StopForeground(true); // overload antigo (bool), obsoleto mas único disponível antes de API 24
#pragma warning restore CS0618
            }

            StopSelf();
        }

        private void RequestLocationUpdates()
        {
            _locationManager?.RequestLocationUpdates(
                LocationManager.GpsProvider,
                minTimeMs: 2000,   // ms
                minDistanceM: 0,   // metros
                this);
        }

        public async void OnLocationChanged(global::Android.Locations.Location location)
        {
            if (_isPaused) return;

            // Ignora fixes GPS de baixa qualidade (comum indoor / multipath)
            if (location.HasAccuracy && location.Accuracy > MaxAcceptableAccuracyMeters)
                return;

            float currentAccuracy = location.HasAccuracy ? location.Accuracy : MaxAcceptableAccuracyMeters;
            var (deltaMeters, deltaSeconds, speedKmh) = ComputeMovementDelta(location);

            await HandleAutoPauseDetectionAsync(speedKmh, deltaSeconds);

            if (_isAutoPaused)
            {
                _lastPoint = location; // mantém referência para o próximo delta ser correto ao retomar
                return; // não acumula distância/tempo/pontos enquanto auto-pausado
            }

            if (IsRealMovement(_lastPoint, deltaMeters, deltaSeconds, speedKmh, location.HasSpeed))
            {
                AccumulateMovement(location, deltaMeters, speedKmh);
            }

            if (location.HasAltitude) _lastAltitude = location.Altitude;

            await PersistTrackPointAsync(location);

            _lastAccuracy = currentAccuracy;
            _lastPoint = location;
        }

        /// <summary>
        /// Calcula distância, tempo decorrido e velocidade desde o último ponto.
        /// Prefere a velocidade Doppler do próprio GPS (<see cref="global::Android.Locations.Location.Speed"/>)
        /// por ser muito mais estável do que derivar velocidade por posição/tempo, sobretudo a baixa velocidade.
        /// </summary>
        private (double deltaMeters, double deltaSeconds, double speedKmh) ComputeMovementDelta(
            global::Android.Locations.Location location)
        {
            if (_lastPoint is null)
                return (0, 0, location.HasSpeed ? location.Speed * 3.6 : 0);

            double deltaMeters = GeoMath.HaversineDistanceMeters(
                _lastPoint.Latitude, _lastPoint.Longitude,
                location.Latitude, location.Longitude);
            double deltaSeconds = (location.Time - _lastPoint.Time) / 1000.0;

            double speedKmh = location.HasSpeed
                ? location.Speed * 3.6
                : deltaSeconds > 0 ? (deltaMeters / deltaSeconds) * 3.6 : 0;

            if (speedKmh > MaxPlausibleSpeedKmh)
                speedKmh = 0; // pico estúpido não conta

            return (deltaMeters, deltaSeconds, speedKmh);
        }

        /// <summary>
        /// Deteta se o deslocamento entre dois fixes é movimento real (vs. ruído de GPS parado).
        /// Quando há velocidade Doppler disponível, confia nela em vez da distância entre fixes —
        /// escalar o limiar pela accuracy (abordagem antiga) tornava o filtro instável: em dias de
        /// receção GPS mediana, o limiar disparava para 30-40m e rejeitava movimento real de ciclismo.
        /// </summary>
        private static bool IsRealMovement(
            global::Android.Locations.Location? lastPoint,
            double deltaMeters,
            double deltaSeconds,
            double speedKmh,
            bool hasSpeed)
        {
            if (lastPoint is null) return true;

            // Corta saltos GPS irrealistas (multipath, salto de satélite)
            if (deltaSeconds > 0 && deltaMeters / deltaSeconds > MaxRealisticInstantSpeedMps)
                return false;

            if (hasSpeed) return speedKmh > MinMovingSpeedKmh;

            return deltaMeters > MinMovementThresholdMeters;
        }

        private void AccumulateMovement(global::Android.Locations.Location location, double deltaMeters, double speedKmh)
        {
            _totalDistanceMeters += deltaMeters;
            if (speedKmh > _maxSpeedKmh) _maxSpeedKmh = speedKmh;

            if (location.HasAltitude && _lastAltitude.HasValue)
            {
                double deltaAltitude = location.Altitude - _lastAltitude.Value;

                if (deltaAltitude > MinElevationDeltaMeters && IsPlausibleElevationDelta(deltaAltitude, deltaMeters))
                {
                    _elevationGainMeters += deltaAltitude;
                }
            }

            WeakReferenceMessenger.Default.Send(new LocationTrackingUpdateMessage(
                _totalDistanceMeters, speedKmh, GetCurrentMovingTimeSeconds(),
                location.HasAltitude ? location.Altitude : null,
                _elevationGainMeters,
                location.Latitude, location.Longitude));
        }

        /// <summary>
        /// Rejeita deltas de altitude cujo gradiente implícito seria fisicamente absurdo para ciclismo.
        /// Protege contra fixes GPS com altitude ainda não estabilizada, comum logo após reaquisição
        /// de sinal — o eixo vertical do GPS demora sempre mais a convergir do que a posição horizontal.
        /// </summary>
        private static bool IsPlausibleElevationDelta(double deltaAltitude, double deltaMeters)
        {
            if (deltaMeters <= 0) return false; // sem deslocamento horizontal, um "salto" vertical é sempre ruído, nunca subida real

            double gradientPercent = (deltaAltitude / deltaMeters) * 100.0;
            return gradientPercent <= MaxRealisticGradientPercent;
        }

        private async Task PersistTrackPointAsync(global::Android.Locations.Location location)
        {
            var point = new LocalTrackPoint
            {
                LocalTrackSegmentId = _currentSegmentId,
                Sequence = _pointSequence++,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                AltitudeMeters = location.HasAltitude ? location.Altitude : null,
                RecordedAt = DateTimeOffset.FromUnixTimeMilliseconds(location.Time).UtcDateTime
            };

            var db = await IPlatformApplication.Current!.Services.GetRequiredService<APP.Services.LocalStorage.ILocalDatabaseService>().GetConnectionAsync();
            await db.InsertAsync(point);
        }

        private async Task HandleAutoPauseDetectionAsync(double speedKmh, double deltaSeconds)
        {
            if (!_autoPauseEnabled) return;

            if (speedKmh < AutoPauseSpeedThresholdKmh)
            {
                _stationarySeconds += deltaSeconds > 0 ? (int)deltaSeconds : 2;

                if (!_isAutoPaused && _stationarySeconds >= AutoPauseAfterSeconds)
                {
                    await CloseCurrentSegmentAsync();
                    _isAutoPaused = true;
                    WeakReferenceMessenger.Default.Send(new LocationTrackingAutoPauseChangedMessage(true));
                    UpdateNotification("Pausado automaticamente");
                }
            }
            else if (speedKmh > AutoResumeSpeedThresholdKmh)
            {
                _stationarySeconds = 0;

                if (_isAutoPaused)
                {
                    _isAutoPaused = false;
                    await OpenNewSegmentAsync();
                    WeakReferenceMessenger.Default.Send(new LocationTrackingAutoPauseChangedMessage(false));
                    UpdateNotification("A gravar percurso...");
                }
            }
        }

        private async Task CloseCurrentSegmentAsync()
        {
            var now = DateTime.UtcNow;

            if (!_isPaused && !_isAutoPaused)
            {
                _accumulatedMovingSeconds += (int)(now - _currentSegmentStartedAt).TotalSeconds;
            }

            var db = await IPlatformApplication.Current!.Services.GetRequiredService<APP.Services.LocalStorage.ILocalDatabaseService>().GetConnectionAsync();
            var segment = await db.Table<LocalTrackSegment>().Where(s => s.Id == _currentSegmentId).FirstOrDefaultAsync();
            if (segment is not null)
            {
                segment.EndedAt = now;
                await db.UpdateAsync(segment);
            }
        }

        private async Task OpenNewSegmentAsync()
        {
            _segmentSequence++;
            _currentSegmentId = Guid.NewGuid();
            _lastPoint = null;
            _currentSegmentStartedAt = DateTime.UtcNow;

            var segment = new LocalTrackSegment
            {
                Id = _currentSegmentId,
                LocalActivityId = _activityId,
                SequenceIndex = _segmentSequence,
                StartedAt = _currentSegmentStartedAt
            };

            var db = await IPlatformApplication.Current!.Services.GetRequiredService<APP.Services.LocalStorage.ILocalDatabaseService>().GetConnectionAsync();
            await db.InsertAsync(segment);
        }

        public void OnProviderDisabled(string provider) { }
        public void OnProviderEnabled(string provider) { }
        public void OnStatusChanged(string? provider, [GeneratedEnum] Availability status, Bundle? extras) { }

        private Notification BuildNotification(string text)
        {
            EnsureChannel();

            return new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("2Cycle")
                .SetContentText(text)
                .SetSmallIcon(Resource.Drawable.notification_icon_background) // ajusta ao teu recurso real
                .SetOngoing(true)
                .Build();
        }

        private void UpdateNotification(string text)
        {
            NotificationManagerCompat.From(this).Notify(NotificationId, BuildNotification(text));
        }

        private void EnsureChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O) return; // canais só existem a partir de Android 8 — NotificationCompat já ignora isto sozinho, mas mantemos o guard para não construirmos o objeto NotificationChannel à toa

            var manager = (NotificationManager)GetSystemService(NotificationService)!;
            if (manager.GetNotificationChannel(ChannelId) is null)
            {
                var channel = new NotificationChannel(ChannelId, "Gravação de Percurso", NotificationImportance.Low);
                manager.CreateNotificationChannel(channel);
            }
        }

        private int GetCurrentMovingTimeSeconds()
        {
            if (_isPaused || _isAutoPaused) return _accumulatedMovingSeconds;
            return _accumulatedMovingSeconds + (int)(DateTime.UtcNow - _currentSegmentStartedAt).TotalSeconds;
        }
    }
}