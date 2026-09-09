using APP.Models;

namespace APP.DTOs.Routes.Responses
{
    public record RouteSuggestionResponse(
        Guid SuggestedRouteId,
        double DistanceMeters,
        double ElevationGainMeters,
        List<RoutePointResponse> Points,
        RouteDifficulty Difficulty
    );

}
