using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class SuggestedRoute
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? CreatedByUserId { get; set; }

        public double OriginLatitude { get; set; }
        public double OriginLongitude { get; set; }
        public double DestinationLatitude { get; set; }
        public double DestinationLongitude { get; set; }

        public double DistanceMeters { get; set; }
        public double ElevationGainMeters { get; set; }

        public string Source { get; set; } = "openrouteservice";

        public bool IsRoundTrip { get; set; } = false;
        public double? RequestedDistanceMeters { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<RoutePoint> Points { get; set; } = new List<RoutePoint>();
        public ICollection<UserSuggestedRoute> SavedByUsers { get; set; } = new List<UserSuggestedRoute>();

        public string PreviewPolylineJson { get; set; } = "[]";

    }
}
