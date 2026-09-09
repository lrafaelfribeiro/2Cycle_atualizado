using API.Core;
using API.DTOs;
using API.Models;
using LIB.Auth;

namespace API.Services.Auth
{
    public interface IAuthService
    {
        public Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
        public Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
        public Task<AuthResponse> GenerateTokens(User user, CancellationToken cancellationToken);
    }
}
