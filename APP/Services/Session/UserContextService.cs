using APP.Services.Token;
using System.Text.Json;

namespace APP.Services.Session
{
    public class UserContextService : IUserContextService
    {
        private readonly ITokenStorageService _tokenStorageService;

        public UserContextService(ITokenStorageService tokenStorageService)
        {
            _tokenStorageService = tokenStorageService;
        }

        public async Task<string?> GetCurrentUserIdAsync()
        {
            var accessToken = await _tokenStorageService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken)) return null;

            var parts = accessToken.Split('.');
            if (parts.Length != 3) return null;

            using var doc = JsonDocument.Parse(DecodeBase64Url(parts[1]));
            return doc.RootElement.TryGetProperty("sub", out var sub) ? sub.GetString() : null;
        }

        public async Task<string?> GetCurrentUserNameAsync()
        {
            var accessToken = await _tokenStorageService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken)) return null;

            var parts = accessToken.Split('.');
            if (parts.Length != 3) return null;

            using var doc = JsonDocument.Parse(DecodeBase64Url(parts[1]));
            return doc.RootElement.TryGetProperty("name", out var name) ? name.GetString() : null;
        }

        private static string DecodeBase64Url(string input)
        {
            string base64 = input.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }
    }
}
