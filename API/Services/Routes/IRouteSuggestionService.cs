using API.Core;
using API.DTOs;
using API.DTOs.Routes;
using API.DTOs.Routes.Requests;
using API.DTOs.Routes.Responses;

namespace API.Services.Routes
{
    /// <summary>
    /// Responsável apenas por SuggestedRoute (rota partilhada, sem dono):
    /// gerar novas sugestões e devolver o detalhe genérico de uma rota (preview).
    /// Tudo o que envolve "o utilizador X guardou esta rota" vive em ISavedRouteService.
    /// </summary>
    public interface IRouteSuggestionService
    {
        Task<Result<RouteSuggestionResponse>> SuggestAsync(Guid userId, RouteSuggestionRequest request, CancellationToken cancellationToken);
        Task<Result<RouteSuggestionResponse>> SuggestRoundTripAsync(Guid userId, RouteRoundTripSuggestionRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Detalhe genérico de uma SuggestedRoute (usado para preview antes de guardar).
        /// Não depende do utilizador — a rota é partilhada — por isso não recebe userId.
        /// </summary>
        Task<Result<RouteDetailResponse>> GetRouteDetailAsync(Guid suggestedRouteId, CancellationToken cancellationToken = default);
        Task<Result<Guid>> PersistPendingRouteAsync(Guid pendingRouteId, CancellationToken cancellationToken = default);
    }
}
