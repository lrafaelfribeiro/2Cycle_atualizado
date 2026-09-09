using API.Core;
using API.DTOs.Routes.Requests;
using API.Services.Routes;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Endpoints sobre SuggestedRoute (rota partilhada, sem dono): gerar sugestões e pré-visualizar.
    /// Tudo o que é "rota guardada por mim" está em SavedRoutesController.
    /// </summary>
    [Route("api/routes")]
    public class RoutesController : AuthenticatedControllerBase
    {
        private readonly IRouteSuggestionService _routeSuggestionService;

        public RoutesController(IRouteSuggestionService routeSuggestionService)
        {
            _routeSuggestionService = routeSuggestionService;
        }

        [HttpPost("suggest")]
        public async Task<IActionResult> Suggest(RouteSuggestionRequest request, CancellationToken cancellationToken)
        {
            var result = await _routeSuggestionService.SuggestAsync(CurrentUserId, request, cancellationToken);
            return result.ToActionResult(this);
        }

        [HttpPost("round-trip")]
        public async Task<IActionResult> SuggestRoundTrip(RouteRoundTripSuggestionRequest request, CancellationToken cancellationToken)
        {
            var result = await _routeSuggestionService.SuggestRoundTripAsync(CurrentUserId, request, cancellationToken);
            return result.ToActionResult(this);
        }

        /// <summary>
        /// Preview genérico de uma rota sugerida — não exige que esteja guardada.
        /// </summary>
        [HttpGet("{suggestedRouteId:guid}")]
        public async Task<IActionResult> GetRouteDetail(Guid suggestedRouteId, CancellationToken cancellationToken)
        {
            var result = await _routeSuggestionService.GetRouteDetailAsync(suggestedRouteId, cancellationToken);
            return result.ToActionResult(this);
        }

    }
}
