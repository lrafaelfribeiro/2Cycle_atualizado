using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class Track
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ActivityId { get; set; }
        public Activity Activity { get; set; } = null!;

        public ICollection<TrackSegment> Segments { get; set; } = new List<TrackSegment>();
    }
}
