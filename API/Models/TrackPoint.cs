using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class TrackPoint
    {
        [Key]
        public long Id { get; set; } 

        public Guid TrackSegmentId { get; set; }
        public TrackSegment TrackSegment { get; set; } = null!;

        public int Sequence { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? AltitudeMeters { get; set; }

        public DateTime RecordedAt { get; set; }
    }
}
