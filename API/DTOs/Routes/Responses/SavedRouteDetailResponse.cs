using API.Models;

namespace API.DTOs.Routes
{
    public record SavedRouteDetailResponse(
        Guid SavedRouteId,
        Guid SuggestedRouteId,
        string? Name,
        double DistanceMeters,
        double ElevationGainMeters,
        double ElevationLossMeters,
        double ElevationNetMeters,
        bool IsFavorite,
        DateTime SavedAt,
        List<RouteDetailPointResponse> Polyline,
        List<ElevationPointResponse> ElevationProfile,
        RouteDifficulty Difficulty
    );
}