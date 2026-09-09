
using SQLite;

namespace APP.Models.Local
{
    [Table("LocalTrackPoints")]
    public class LocalTrackPoint
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; } // bigint autoincrement, tal como no backend

        [Indexed]
        public Guid LocalTrackSegmentId { get; set; }

        public int Sequence { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? AltitudeMeters { get; set; }

        public DateTime RecordedAt { get; set; }
    }
}
