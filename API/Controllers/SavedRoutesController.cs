using API.Core;
using API.DTOs.Routes.Requests;
using API.Services.Routes;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Endpoints sobre UserSuggestedRoute — as rotas que ESTE utilizador guardou.
    /// Identificador de URL é sempre suggestedRouteId (Id da SuggestedRoute), nunca o Id
    /// interno da linha UserSuggestedRoute — evita a ambiguidade que existia antes.
    /// </summary>
    [Route("api/routes/saved")]
    public class SavedRoutesController : AuthenticatedControllerBase
    {
        private readonly ISavedRouteService _savedRouteService;

        public SavedRoutesController(ISavedRouteService savedRouteService)
        {
            _savedRouteService = savedRouteService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSaved([FromQuery] bool favoritesOnly, CancellationToken cancellationToken)
        {
            var result = await _savedRouteService.GetSavedAsync(CurrentUserId, favoritesOnly, cancellationToken);
            return result.ToActionResult(this);
        }

        [HttpGet("{suggestedRouteId:guid}")]
        public async Task<IActionResult> GetSavedDetail(Guid suggestedRouteId, CancellationToken cancellationToken)
        {
            var result = await _savedRouteService.GetSavedRouteDetailAsync(CurrentUserId, suggestedRouteId, cancellationToken);
            return result.ToActionResult(this);
        }

        [HttpPost("{suggestedRouteId:guid}")]
        public async Task<IActionResult> Save(Guid suggestedRouteId, SaveRouteRequest request, CancellationToken cancellationToken)
        {
            var result = await _savedRouteService.SaveAsync(CurrentUserId, suggestedRouteId, request, cancellationToken);
            return result.ToActionResult(this);
        }

        [HttpDelete("{suggestedRouteId:guid}")]
        public async Task<IActionResult> Unsave(Guid suggestedRouteId, CancellationToken cancellationToken)
        {
            var result = await _savedRouteService.UnsaveAsync(CurrentUserId, suggestedRouteId, cancellationToken);
            return result.ToActionResult(this);
        }

        [HttpPost("{suggestedRouteId:guid}/favorite")]
        public async Task<IActionResult> Favorite(Guid suggestedRouteId, CancellationToken cancellationToken)
        {
            var result = await _savedRouteService.SetFavoriteAsync(CurrentUserId, suggestedRouteId, isFavorite: true, cancellationToken);
            return result.ToActionResult(this);
        }

        [HttpDelete("{suggestedRouteId:guid}/favorite")]
        public async Task<IActionResult> Unfavorite(Guid suggestedRouteId, CancellationToken cancellationToken)
        {
            var result = await _savedRouteService.SetFavoriteAsync(CurrentUserId, suggestedRouteId, isFavorite: false, cancellationToken);
            return result.ToActionResult(this);
        }

        [HttpPatch("{suggestedRouteId:guid}/name")]
        public async Task<IActionResult> RenameSavedRoute(Guid suggestedRouteId, RenameRouteRequest request)
        {
            var result = await _savedRouteService.RenameAsync(CurrentUserId, suggestedRouteId, request.Name);
            return result.ToActionResult(this);
        }
    }
}
