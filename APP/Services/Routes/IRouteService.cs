using APP.DTOs.Routes;
using APP.DTOs.Routes.Requests;
using APP.DTOs.Routes.Responses;

namespace APP.Services.Routes
{
    public interface IRouteService
    {
        Task<RouteSuggestionResponse> SuggestAsync(RouteSuggestionRequest request, CancellationToken cancellationToken = default);
        Task<RouteSuggestionResponse> SuggestRoundTripAsync(RouteRoundTripSuggestionRequest request, CancellationToken cancellationToken = default);

        /// <summary>Preview genérico de uma rota (não exige que esteja guardada).</summary>
        Task<RouteDetailResponse> GetRouteDetailAsync(Guid suggestedRouteId, CancellationToken cancellationToken = default);

        /// <summary>Guarda (ou renomeia, se já existir) uma rota para o utilizador atual. Nunca mexe no favorito.</summary>
        Task SaveAsync(Guid suggestedRouteId, string? name, CancellationToken cancellationToken = default);
        Task UnsaveAsync(Guid suggestedRouteId, CancellationToken cancellationToken = default);

        /// <summary>Marca/desmarca como favorita — a rota já tem de estar guardada.</summary>
        Task SetFavoriteAsync(Guid suggestedRouteId, bool isFavorite, CancellationToken cancellationToken = default);

        Task<List<SavedRouteSummaryResponse>> GetSavedAsync(bool favoritesOnly, CancellationToken cancellationToken = default);
        Task<SavedRouteDetailResponse> GetSavedRouteDetailAsync(Guid suggestedRouteId, CancellationToken cancellationToken = default);
        Task RenameAsync(Guid suggestedRouteId, string name, CancellationToken cancellationToken = default);
    }
}