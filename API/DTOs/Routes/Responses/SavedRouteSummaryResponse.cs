using API.Models;

namespace API.DTOs.Routes.Responses
{
    public record SavedRouteSummaryResponse(
            Guid Id,
            Guid SuggestedRouteId,
            string Name,
            double DistanceMeters,
            double ElevationGainMeters,
            bool IsFavorite,
            DateTime SavedAt,
            List<PreviewPointResponse> PreviewPoints,
            RouteDifficulty Difficulty
    );

    public record PreviewPointResponse(double Latitude, double Longitude);
}