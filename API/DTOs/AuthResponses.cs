namespace LIB.Auth
{
    public record AuthResponse(

        string AccessToken,

        string RefreshToken,

        DateTimeOffset ExpiresAt,

        string TokenType = "Bearer"

    );

    public record UserResponse(

        Guid Id,

        string Name,

        string Email

    );
}