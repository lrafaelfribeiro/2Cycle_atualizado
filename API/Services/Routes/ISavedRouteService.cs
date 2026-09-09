using API.Core;
using API.DTOs.Routes;
using API.DTOs.Routes.Requests;
using API.DTOs.Routes.Responses;

namespace API.Services.Routes
{
    /// <summary>
    /// Responsável apenas por UserSuggestedRoute (a relação "este utilizador guardou esta rota").
    /// Todos os métodos são scoped por userId e identificam a rota sempre por suggestedRouteId
    /// (o Id da SuggestedRoute) — nunca pelo Id interno da própria UserSuggestedRoute.
    /// </summary>
    public interface ISavedRouteService
    {
        Task<Result> SaveAsync(Guid userId, Guid suggestedRouteId, SaveRouteRequest request, CancellationToken cancellationToken);
        Task<Result> UnsaveAsync(Guid userId, Guid suggestedRouteId, CancellationToken cancellationToken);

        /// <summary>
        /// Marca/desmarca como favorita uma rota já guardada. Separado de SaveAsync de propósito:
        /// "guardar/dar nome" e "marcar favorita" são ações distintas, e assim evitas ter de reenviar
        /// o nome só para alternar o favorito.
        /// </summary>
        Task<Result> SetFavoriteAsync(Guid userId, Guid suggestedRouteId, bool isFavorite, CancellationToken cancellationToken);

        Task<Result<List<SavedRouteSummaryResponse>>> GetSavedAsync(Guid userId, bool favoritesOnly, CancellationToken cancellationToken);
        Task<Result<SavedRouteDetailResponse>> GetSavedRouteDetailAsync(Guid userId, Guid suggestedRouteId, CancellationToken cancellationToken = default);
        Task<Result> RenameAsync(Guid userId, Guid suggestedRouteId, string routeName, CancellationToken cancellationToken = default);
    }
}
