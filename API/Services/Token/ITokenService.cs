using API.Models;

namespace API.Services.Token
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        string HashRefreshToken(string rawToken);
    }
}
