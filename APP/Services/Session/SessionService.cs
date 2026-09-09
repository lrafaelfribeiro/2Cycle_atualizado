using APP.Services.Auth;
using APP.Services.Token;

namespace APP.Services.Session
{
    public class SessionService : ISessionService
    {
        private readonly IAuthService _authService;
        private readonly ITokenStorageService _tokenStorageService;

        public SessionService(IAuthService authService, ITokenStorageService tokenStorageService)
        {
            _authService = authService;
            _tokenStorageService = tokenStorageService;
        }
        public async Task<bool> EnsureSessionIsValidAsync(CancellationToken cancellationToken)
        {
            var accessToken = await _tokenStorageService.GetAccessTokenAsync();

            if (accessToken is null)
                return false;

            // Verificar se o token ainda e valido
            var tokenTime = await _tokenStorageService.GetExpiresAtAsync();
            if (tokenTime > DateTime.UtcNow)
                return true;

            // Verificar se existe refresh
            var refreshToken = await _tokenStorageService.GetRefreshTokenAsync();
            if (refreshToken is null)
                return false;
            
            // Tentar dar refresh
            var result = await _authService.RefreshTokensAsync(refreshToken, cancellationToken);
            if (result.IsSuccess)
                return true;

            // Remover todos os tokens
            await _tokenStorageService.ClearTokensAsync();
            return false;
        }
    }
}