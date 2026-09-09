using APP.DTOs.Routes.Responses;
using APP.Models;

namespace APP.DTOs.Routes
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


}
