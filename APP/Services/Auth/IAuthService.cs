using APP.Core;
using APP.Models;

namespace APP.Services.Auth
{
    public interface IAuthService
    {
        Task<Result<AuthResponseDTO>> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken);
        Task<Result<AuthResponseDTO>> LoginAsync(string email, string password, CancellationToken cancellationToken);
        Task<Result<AuthResponseDTO>> RefreshTokensAsync(string refreshToken, CancellationToken cancellationToken);
    }
}
