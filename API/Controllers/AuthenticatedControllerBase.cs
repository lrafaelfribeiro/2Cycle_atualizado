using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Base para qualquer controller que precise do Id do utilizador autenticado.
    /// Reaproveitada por RoutesController, SavedRoutesController e futuros controllers
    /// (Activities, Profile, etc.) — evita repetir a mesma property em cada um.
    /// </summary>
    [ApiController]
    [Authorize]
    public abstract class AuthenticatedControllerBase : ControllerBase
    {
        protected Guid CurrentUserId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    }
}
