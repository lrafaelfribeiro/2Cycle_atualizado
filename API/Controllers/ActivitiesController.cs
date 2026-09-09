using API.Core;
using API.DTOs;
using API.DTOs.Actitivities.Requests;
using API.Services.Activities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("/api/activities")]
    [Authorize]
    public class ActivitiesController : AuthenticatedControllerBase
    {
        private const long MaxActivityRequestBodyBytes = 5_000_000;
        private readonly IActivityService _activityService;

        public ActivitiesController(IActivityService activityService)
        {
            _activityService = activityService;
        }

        [HttpPost]
        [RequestSizeLimit(MaxActivityRequestBodyBytes)]
        public async Task<IActionResult> Create(CreateActivityRequest request, CancellationToken cancellationToken)
        {
            var result = await _activityService.CreateAsync(CurrentUserId, request, cancellationToken);
            return result.ToActionResult(this);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? days, CancellationToken cancellationToken)
        {

            if (days is <= 0)
            {
                return Problem(
                    title: "O parâmetro 'days' deve ser um número positivo.",
                    statusCode: StatusCodes.Status400BadRequest,
                    type: "urn:api:errors:INVALID_QUERY_PARAM");
            }

            var result = await _activityService.GetByUserAsync(CurrentUserId, days, cancellationToken);
            return result.ToActionResult(this);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDetail(Guid id, CancellationToken cancellationToken)
        {
            var result = await _activityService.GetDetailByIdAsync(CurrentUserId, id, cancellationToken);
            return result.ToActionResult(this);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var result = await _activityService.DeleteAsync(CurrentUserId, id, cancellationToken);
            return result.ToActionResult(this);
        }
    }
}
