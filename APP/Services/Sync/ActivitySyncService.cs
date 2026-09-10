using APP.DTOs;
using APP.DTOs.Activities.Responses;
using APP.Models.Local;
using APP.Services.LocalStorage;
using APP.Services.Session;
using System.Net.Http.Json;

namespace APP.Services.Sync
{
    public class ActivitySyncService : IActivitySyncService
    {
        private readonly IActivityLocalRepository _repository;
        private readonly IUserContextService _userContextService;
        private readonly HttpClient _httpClient;

        public ActivitySyncService(IActivityLocalRepository repository, IUserContextService userContextService, HttpClient httpClient)
        {
            _repository = repository;
            _userContextService = userContextService;
            _httpClient = httpClient;
        }

        public async Task SyncPendingActivitiesAsync()
        {
            var userId = await _userContextService.GetCurrentUserIdAsync();
            if (userId is null) return;

            var pending = await _repository.GetPendingSyncActivitiesAsync(userId);

            foreach (var activity in pending)
            {
                try
                {
                    var segments = await _repository.GetSegmentsAsync(activity.Id);
                    var segmentRequests = new List<SyncTrackSegmentRequest>();

                    foreach (var segment in segments)
                    {
                        var points = await _repository.GetPointsAsync(segment.Id);
                        segmentRequests.Add(new SyncTrackSegmentRequest(
                            segment.SequenceIndex,
                            segment.StartedAt,
                            segment.EndedAt ?? segment.StartedAt,
                            points.Select(p => new SyncTrackPointRequest(
                                p.Sequence, p.Latitude, p.Longitude, p.AltitudeMeters, p.RecordedAt)).ToList()
                        ));
                    }

                    var request = new SyncActivityRequest(
                        activity.Id,
                        activity.StartedAt,
                        activity.EndedAt ?? activity.StartedAt,
                        activity.DistanceMeters,
                        activity.TotalTimeSeconds,
                        activity.MovingTimeSeconds,
                        activity.AverageSpeedKmh,
                        activity.MaxSpeedKmh,
                        activity.ElevationGainMeters,
                        segmentRequests
                    );

                    var response = await _httpClient.PostAsJsonAsync("api/activities", request);

                    if (response.IsSuccessStatusCode)
                    {
                        await _repository.MarkAsSyncedAsync(activity.Id);
                    }
                    else if ((int)response.StatusCode is >= 400 and < 500)
                    {
                        // Erro do próprio pedido (validação, conflito) — retry automático não resolveria nada
                        await _repository.MarkAsFailedAsync(activity.Id);
                    }
                    // 5xx / exceção de rede: fica Pending, tenta-se de novo depois
                }
                catch
                {
                    // Sem rede / timeout — mantém Pending, próxima tentativa resolve
                }
            }
        }
        public async Task PullRemoteActivitiesAsync()
        {
            var userId = await _userContextService.GetCurrentUserIdAsync();
            if (userId is null) return;

            try
            {
                var remoteActivities = await _httpClient.GetFromJsonAsync<List<ActivityResponse>>("api/activities");
                if (remoteActivities is null) return;

                foreach (var remote in remoteActivities)
                {
                    if (await _repository.ExistsAsync(remote.Id))
                        continue; // já a temos (gravada neste dispositivo, ou já puxada antes)

                    await _repository.InsertRemoteSummaryAsync(MapToLocalSummary(remote, userId));

                    // Traz logo o track desta atividade nova. Sem isto, a lista não tem
                    // pontos GPS para gerar o mapa até o utilizador abrir o detalhe (o
                    // lazy load original só existia para poupar dados ao ABRIR uma
                    // atividade específica) — o que faz o thumbnail parecer "em falta"
                    // logo na primeira sincronização entre dispositivos. Só corre para
                    // atividades novas (o continue acima já filtra as conhecidas), por
                    // isso o custo de rede extra fica limitado ao necessário.
                    await EnsureTrackDownloadedAsync(remote.Id);
                }
            }
            catch
            {
                // Sem rede — mantém-se o histórico local já conhecido
            }
        }

        public async Task EnsureTrackDownloadedAsync(Guid activityId)
        {
            if (await _repository.HasTrackDataAsync(activityId)) return;

            try
            {
                var detail = await _httpClient.GetFromJsonAsync<ActivityDetailResponse>($"api/activities/{activityId}");
                if (detail is null) return;

                var (segments, points) = MapToLocalTrack(activityId, detail.Points);
                await _repository.SaveTrackDetailAsync(activityId, segments, points);
            }
            catch
            {
                // Resumo continua visível; mapa fica vazio até próxima tentativa
            }
        }

        public async Task SyncAsync()
        {
            await SyncPendingActivitiesAsync();
            await ProcessPendingDeletionsAsync();
            await PullRemoteActivitiesAsync();
        }

        private static LocalActivity MapToLocalSummary(ActivityResponse remote, string ownerUserId) => new()
        {
            Id = remote.Id,
            OwnerUserId = ownerUserId,
            StartedAt = remote.StartedAt,
            EndedAt = remote.EndedAt,
            DistanceMeters = remote.DistanceMeters,
            TotalTimeSeconds = remote.TotalTimeSeconds,
            MovingTimeSeconds = remote.MovingTimeSeconds,
            AverageSpeedKmh = remote.AverageSpeedKmh,
            MaxSpeedKmh = remote.MaxSpeedKmh,
            ElevationGainMeters = remote.ElevationGainMeters,
            SyncStatus = SyncStatus.Synced,
            CreatedAt = DateTime.UtcNow
        };

        private static (List<LocalTrackSegment> Segments, List<LocalTrackPoint> Points) MapToLocalTrack(
            Guid activityId, List<TrackPointResponse> remotePoints)
        {
            var segments = remotePoints
                .Select(p => p.SegmentSequenceIndex)
                .Distinct()
                .Select(seqIndex => new LocalTrackSegment
                {
                    Id = Guid.NewGuid(),
                    LocalActivityId = activityId,
                    SequenceIndex = seqIndex,
                    StartedAt = remotePoints.Where(p => p.SegmentSequenceIndex == seqIndex).Min(p => p.RecordedAt),
                    EndedAt = remotePoints.Where(p => p.SegmentSequenceIndex == seqIndex).Max(p => p.RecordedAt)
                })
                .ToList();

            var points = new List<LocalTrackPoint>();
            foreach (var segment in segments)
            {
                int sequence = 0;
                foreach (var p in remotePoints.Where(p => p.SegmentSequenceIndex == segment.SequenceIndex).OrderBy(p => p.RecordedAt))
                {
                    points.Add(new LocalTrackPoint
                    {
                        LocalTrackSegmentId = segment.Id,
                        Sequence = sequence++,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        AltitudeMeters = p.AltitudeMeters,
                        RecordedAt = p.RecordedAt
                    });
                }
            }

            return (segments, points);
        }
        public async Task ProcessPendingDeletionsAsync()
        {
            var userId = await _userContextService.GetCurrentUserIdAsync();
            if (userId is null) return;

            var pendingDeletions = await _repository.GetPendingDeletionActivitiesAsync(userId);

            foreach (var activity in pendingDeletions)
            {
                if (await TryDeleteRemoteAsync(activity.Id))
                    await _repository.DeleteActivityAsync(activity.Id); // confirmado no servidor → agora sim, hard delete local
                                                                        // se falhar, mantém-se PendingDeletion — tenta-se de novo no próximo SyncAsync()
            }
        }

        private async Task<bool> TryDeleteRemoteAsync(Guid activityId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/activities/{activityId}");
                return response.IsSuccessStatusCode; // 200/204 — o endpoint já é idempotente, não precisa de tratar 404 à parte
            }
            catch
            {
                return false; // sem rede — mantém PendingDeletion
            }
        }

    }
}
