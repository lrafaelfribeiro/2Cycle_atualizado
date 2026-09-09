
using SQLite;

namespace APP.Models.Local
{
    [Table("LocalTrackSegments")]
    public class LocalTrackSegment
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Indexed]
        public Guid LocalActivityId { get; set; }

        public int SequenceIndex { get; set; } // ordem do segmento dentro do Track

        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }
}
