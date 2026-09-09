using API.Models;

namespace API.DTOs.Routes
{
    public record RouteDetailResponse(
        Guid SuggestedRouteId,
        double DistanceMeters,
        double ElevationGainMeters,
        double ElevationLossMeters,
        double ElevationNetMeters,
        List<RouteDetailPointResponse> Polyline,
        List<ElevationPointResponse> ElevationProfile,
        RouteDifficulty Difficulty
    );

    public record RouteDetailPointResponse(double Latitude, double Longitude);
    public record ElevationPointResponse(double DistanceMeters, double AltitudeMeters);
    public record ElevationSummary(double Gain, double Loss, double Net, List<ElevationPointResponse> Profile);
}