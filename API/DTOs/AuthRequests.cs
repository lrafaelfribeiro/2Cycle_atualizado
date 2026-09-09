using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public record RegisterRequest(
        string Name,

        string Email,

        string Password
    );

    public record LoginRequest(
        string Email,

        string Password
    );

    public record RefreshRequest(
        [Required, MinLength(10)]
        string RefreshToken
    );
}
