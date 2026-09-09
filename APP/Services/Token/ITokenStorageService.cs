namespace APP.Services.Token
{
    public interface ITokenStorageService
    {
        Task SaveTokensAsync(string accessToken, string refreshToken, DateTime expiresAt);
        Task<string?> GetAccessTokenAsync();
        Task<string?> GetRefreshTokenAsync();
        Task<DateTime?> GetExpiresAtAsync();
        Task ClearTokensAsync();
    }
}
