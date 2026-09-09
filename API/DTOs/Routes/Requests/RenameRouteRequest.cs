using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Routes.Requests
{
    public record RenameRouteRequest(
            [Required, MaxLength(100)]
        string Name
        );
}