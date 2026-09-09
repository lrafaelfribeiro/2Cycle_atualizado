namespace APP.Models
{
    public sealed record AuthResponseDTO(string accessToken, string refreshToken, DateTime expiresAt, string tokenType);
}
