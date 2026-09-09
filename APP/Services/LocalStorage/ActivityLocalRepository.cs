using APP.Models.Local;

namespace APP.Services.LocalStorage
{
    public class ActivityLocalRepository : IActivityLocalRepository
    {
        private readonly ILocalDatabaseService _dbService;

        public ActivityLocalRepository(ILocalDatabaseService dbService)
        {
            _dbService = dbService;
        }

        public async Task SaveActivityAsync(LocalActivity activity, List<LocalTrackSegment> segments, List<LocalTrackPoint> points)
        {
            var connection = await _dbService.GetConnectionAsync();

            // Transação: ou grava tudo (activity + segmentos + pontos) ou nada.
            // Evita atividade "órfã" sem pontos se o processo for interrompido a meio.
            await connection.RunInTransactionAsync(tran =>
            {
                tran.Insert(activity);
                tran.InsertAll(segments);
                tran.InsertAll(points);
            });
        }

        public async Task<List<LocalActivity>> GetPendingSyncActivitiesAsync(string ownerUserId)
        {
            var connection = await _dbService.GetConnectionAsync();
            return await connection.Table<LocalActivity>()
                .Where(a => a.OwnerUserId == ownerUserId && a.SyncStatus == SyncStatus.Pending)
                .ToListAsync();
        }

        public async Task<List<LocalTrackSegment>> GetSegmentsAsync(Guid activityId)
        {
            var connection = await _dbService.GetConnectionAsync();
            return await connection.Table<LocalTrackSegment>()
                .Where(s => s.LocalActivityId == activityId)
                .OrderBy(s => s.SequenceIndex)
                .ToListAsync();
        }

        public async Task<List<LocalTrackPoint>> GetPointsAsync(Guid segmentId)
        {
            var connection = await _dbService.GetConnectionAsync();
            return await connection.Table<LocalTrackPoint>()
                .Where(p => p.LocalTrackSegmentId == segmentId)
                .OrderBy(p => p.Sequence)
                .ToListAsync();
        }

        public async Task MarkAsSyncedAsync(Guid activityId)
        {
            var connection = await _dbService.GetConnectionAsync();
            var activity = await connection.Table<LocalActivity>().Where(a => a.Id == activityId).FirstOrDefaultAsync();
            if (activity is null) return;

            activity.SyncStatus = SyncStatus.Synced;
            await connection.UpdateAsync(activity);
        }

        public async Task MarkAsFailedAsync(Guid activityId)
        {
            var connection = await _dbService.GetConnectionAsync();
            var activity = await connection.Table<LocalActivity>().Where(a => a.Id == activityId).FirstOrDefaultAsync();
            if (activity is null) return;

            activity.SyncStatus = SyncStatus.Failed;
            await connection.UpdateAsync(activity);
        }

        public async Task<LocalActivity?> GetActivityByIdAsync(Guid activityId)
        {
            var connection = await _dbService.GetConnectionAsync();
            return await connection.Table<LocalActivity>().Where(a => a.Id == activityId).FirstOrDefaultAsync();
        }

        public async Task<List<LocalActivity>> GetAllActivitiesAsync(string ownerUserId)
        {
            var connection = await _dbService.GetConnectionAsync();
            return await connection.Table<LocalActivity>()
                .Where(a => a.OwnerUserId == ownerUserId && a.SyncStatus != SyncStatus.PendingDeletion)
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync();
        }
        public async Task DeleteActivityAsync(Guid activityId)
        {
            var connection = await _dbService.GetConnectionAsync();

            var segments = await connection.Table<LocalTrackSegment>()
                .Where(s => s.LocalActivityId == activityId).ToListAsync();

            foreach (var segment in segments)
            {
                var points = await connection.Table<LocalTrackPoint>()
                    .Where(p => p.LocalTrackSegmentId == segment.Id).ToListAsync();

                foreach (var point in points)
                    await connection.DeleteAsync(point);

                await connection.DeleteAsync(segment);
            }

            var activity = await connection.Table<LocalActivity>()
                .Where(a => a.Id == activityId).FirstOrDefaultAsync();

            if (activity is not null)
                await connection.DeleteAsync(activity);
        }

        public async Task<bool> ExistsAsync(Guid activityId)
        {
            var connection = await _dbService.GetConnectionAsync();
            return await connection.Table<LocalActivity>().Where(a => a.Id == activityId).CountAsync() > 0;
        }

        public async Task InsertRemoteSummaryAsync(LocalActivity activity)
        {
            var connection = await _dbService.GetConnectionAsync();
            await connection.InsertAsync(activity);
        }

        public async Task<bool> HasTrackDataAsync(Guid activityId)
        {
            var connection = await _dbService.GetConnectionAsync();
            return await connection.Table<LocalTrackSegment>().Where(s => s.LocalActivityId == activityId).CountAsync() > 0;
        }

        public async Task SaveTrackDetailAsync(Guid activityId, List<LocalTrackSegment> segments, List<LocalTrackPoint> points)
        {
            var connection = await _dbService.GetConnectionAsync();
            await connection.RunInTransactionAsync(tran =>
            {
                tran.InsertAll(segments);
                tran.InsertAll(points);
            });
        }
        public async Task MarkAsPendingDeletionAsync(Guid activityId)
        {
            var connection = await _dbService.GetConnectionAsync();
            var activity = await connection.Table<LocalActivity>().Where(a => a.Id == activityId).FirstOrDefaultAsync();
            if (activity is null) return;

            activity.SyncStatus = SyncStatus.PendingDeletion;
            await connection.UpdateAsync(activity);
        }

        public async Task<List<LocalActivity>> GetPendingDeletionActivitiesAsync(string ownerUserId)
        {
            var connection = await _dbService.GetConnectionAsync();
            return await connection.Table<LocalActivity>()
                .Where(a => a.OwnerUserId == ownerUserId && a.SyncStatus == SyncStatus.PendingDeletion)
                .ToListAsync();
        }

    }
}
