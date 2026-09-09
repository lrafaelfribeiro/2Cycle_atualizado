using API.Core;
using Microsoft.AspNetCore.RateLimiting;
using API.DTOs;
using API.Services.Auth;
using API.Services.RefreshTokens;
using LIB.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IRefreshTokenService _refreshTokenService;

        public AuthController(IAuthService authService, IRefreshTokenService refreshTokenService)
        {
            _authService = authService;
            _refreshTokenService = refreshTokenService;
        }

        [HttpPost("register")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthRegister)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);

            return result.ToActionResult(this);
        }

        [HttpPost("login")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthLogin)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.LoginAsync(request, cancellationToken);

            return result.ToActionResult(this);
        }

        [HttpPost("refresh")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthRefresh)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
        {
            var result = await _refreshTokenService.RotateAsync(request.RefreshToken, cancellationToken);

            return result.ToActionResult(this);
        }

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
        {
            string userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

            await _refreshTokenService.RevokeAsync(request.RefreshToken, userId, cancellationToken);
            return NoContent();
        }

    }
}
