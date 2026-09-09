using APP.DTOs.Routes.Responses;
using APP.Models;

namespace APP.DTOs.Routes
{
    // Item da lista de rotas guardadas. ThumbnailPath não vem do backend — é preenchido
    // localmente depois (ver RoutesViewModel.LoadAsync + IRouteThumbnailService).
    public record SavedRouteSummaryResponse(
        Guid Id,
        Guid SuggestedRouteId,
        string? Name,
        double DistanceMeters,
        double ElevationGainMeters,
        bool IsFavorite,
        DateTime SavedAt,
        List<PreviewPointResponse> PreviewPoints,
        RouteDifficulty Difficulty
    )
    {
        public string? ThumbnailPath { get; init; }
    }


}
