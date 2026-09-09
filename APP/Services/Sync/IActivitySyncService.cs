using System;
using System.Collections.Generic;
using System.Text;

namespace APP.Services.Sync
{
    public interface IActivitySyncService
    {
        Task SyncPendingActivitiesAsync();
        Task PullRemoteActivitiesAsync();
        Task EnsureTrackDownloadedAsync(Guid activityId);
        Task SyncAsync();
        Task ProcessPendingDeletionsAsync();
    }
}
