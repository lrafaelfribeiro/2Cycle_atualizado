using API.Core;
using API.DTOs;
using API.Services.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("/api/profiles")]
    [Authorize]
    public class ProfilesController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfilesController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

        [HttpGet("me")]
        public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
        {
            var result = await _profileService.GetProfileByIdAsync(CurrentUserId, cancellationToken);
            return result.ToActionResult(this);
        }

        [HttpPost("me")]
        public async Task<IActionResult> Create(CreateProfileRequest request, CancellationToken cancellationToken)
        {
            var result = await _profileService.CreateProfileAsync(CurrentUserId, request, cancellationToken);
            return result.ToActionResult(this);
        }

        [HttpPut("me")]
        public async Task<IActionResult> Update(UpdateProfileRequest request, CancellationToken cancellationToken)
        {
            var result = await _profileService.UpdateProfileAsync(CurrentUserId, request, cancellationToken);
            return result.ToActionResult(this);
        }

        [HttpPost("me/weight-logs")]
        public async Task<IActionResult> AddWeightLog(AddWeightLogRequest request, CancellationToken cancellationToken)
        {
            var result = await _profileService.AddWeightLogAsync(CurrentUserId, request, cancellationToken);
            return result.ToActionResult(this);
        }

        [HttpGet("me/weight-logs")]
        public async Task<IActionResult> GetWeightHistory(CancellationToken cancellationToken)
        {
            var result = await _profileService.GetWeightHistoryAsync(CurrentUserId, cancellationToken);
            return result.ToActionResult(this);
        }
    }
}
