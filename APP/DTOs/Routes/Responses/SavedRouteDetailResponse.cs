using APP.Models;

namespace APP.DTOs.Routes.Responses
{
    // Detalhe completo de UMA rota guardada por este utilizador (tem Name/IsFavorite/SavedAt,
    // ao contrário do RouteDetailResponse genérico).
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