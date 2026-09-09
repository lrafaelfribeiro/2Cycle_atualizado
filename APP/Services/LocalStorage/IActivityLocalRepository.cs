using APP.Models.Local;

namespace APP.Services.LocalStorage
{
    public interface IActivityLocalRepository
    {
        Task SaveActivityAsync(LocalActivity activity, List<LocalTrackSegment> segments, List<LocalTrackPoint> points);
        Task<List<LocalActivity>> GetPendingSyncActivitiesAsync(string ownerUserId);
        Task<List<LocalTrackSegment>> GetSegmentsAsync(Guid activityId);
        Task<List<LocalTrackPoint>> GetPointsAsync(Guid segmentId);
        Task MarkAsSyncedAsync(Guid activityId);
        Task MarkAsFailedAsync(Guid activityId);
        Task<LocalActivity?> GetActivityByIdAsync(Guid activityId);
        Task<List<LocalActivity>> GetAllActivitiesAsync(string ownerUserId);
        Task DeleteActivityAsync(Guid activityId);
        Task<bool> ExistsAsync(Guid activityId);
        Task InsertRemoteSummaryAsync(LocalActivity activity);
        Task<bool> HasTrackDataAsync(Guid activityId);
        Task SaveTrackDetailAsync(Guid activityId, List<LocalTrackSegment> segments, List<LocalTrackPoint> points);
        Task MarkAsPendingDeletionAsync(Guid activityId);
        Task<List<LocalActivity>> GetPendingDeletionActivitiesAsync(string ownerUserId);
    }
}
