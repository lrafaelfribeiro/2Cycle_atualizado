using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class Activity
    {
        [Key]
        public Guid Id { get; set; } 
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime StartedAt { get; set; }
        public DateTime EndedAt { get; set; }

        public double DistanceMeters { get; set; }
        public int TotalTimeSeconds { get; set; }
        public int MovingTimeSeconds { get; set; }
        public double AverageSpeedKmh { get; set; }
        public double MaxSpeedKmh { get; set; }
        public double ElevationGainMeters { get; set; }

        public Track Track { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
