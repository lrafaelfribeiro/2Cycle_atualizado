using API.Core;
using LIB.Auth;

namespace API.Services.RefreshTokens
{
    public interface IRefreshTokenService
    {
        public Task<string> CreateAsync(Guid userId, Guid? familyId, CancellationToken cancellationToken);
        public Task<Result<AuthResponse>> RotateAsync(string oldTokenValue, CancellationToken cancellationToken);
        public Task RevokeAsync(string tokenValue, string userId, CancellationToken cancellationToken);
    }
}
