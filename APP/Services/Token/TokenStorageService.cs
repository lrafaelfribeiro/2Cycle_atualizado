namespace APP.Services.Token
{
    public class TokenStorageService : ITokenStorageService
    {
        public async Task SaveTokensAsync(string accessToken, string refreshToken, DateTime expiresAt)
        {
            await SecureStorage.SetAsync("access_token", accessToken);
            await SecureStorage.SetAsync("refresh_token", refreshToken);
            await SecureStorage.SetAsync("expires_at", expiresAt.ToString("O")); // "O" Usa o formato fixo 
        }
        public async Task<string?> GetAccessTokenAsync()
        {
            return await SecureStorage.GetAsync("access_token");
        }
        public async Task<string?> GetRefreshTokenAsync()
        {
            return await SecureStorage.GetAsync("refresh_token");
        }
        public async Task<DateTime?> GetExpiresAtAsync()
        {
            var raw = await SecureStorage.GetAsync("expires_at");
            if (string.IsNullOrEmpty(raw))
            {
                return null;
            }
            return DateTime.Parse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind);
        }
        public Task ClearTokensAsync()
        {
            // Remover apenas os tokens
            SecureStorage.Remove("access_token");
            SecureStorage.Remove("refresh_token");
            SecureStorage.Remove("expires_at");

            return Task.CompletedTask;
        }
    }
}
