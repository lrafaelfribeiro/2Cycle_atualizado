using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class RoutePoint
    {
            [Key]
            public long Id { get; set; }
            public Guid SuggestedRouteId { get; set; }
            public SuggestedRoute SuggestedRoute { get; set; } = null!;

            public int Sequence { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double? AltitudeMeters { get; set; }
    }
}
