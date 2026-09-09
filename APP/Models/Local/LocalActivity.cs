
using SQLite;

namespace APP.Models.Local
{
    public enum SyncStatus
    {
        Pending,
        Synced,
        Failed,
        PendingDeletion
    }

    [Table("LocalActivities")]
    public class LocalActivity
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid(); // idempotency key para o backend

        [Indexed]
        public string OwnerUserId { get; set; } = string.Empty; // isolamento por conta local

        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        public double DistanceMeters { get; set; }
        public int TotalTimeSeconds { get; set; }
        public int MovingTimeSeconds { get; set; }
        public double AverageSpeedKmh { get; set; }
        public double MaxSpeedKmh { get; set; }
        public double ElevationGainMeters { get; set; }

        [Indexed]
        public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }

}
