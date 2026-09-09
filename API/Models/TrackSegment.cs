using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class TrackSegment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TrackId { get; set; }
        public Track Track { get; set; } = null!;

        public int SequenceIndex { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime EndedAt { get; set; }

        public ICollection<TrackPoint> Points { get; set; } = new List<TrackPoint>();
    }
}
