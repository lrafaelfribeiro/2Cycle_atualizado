using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class UserSuggestedRoute
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid SuggestedRouteId { get; set; }
        public SuggestedRoute SuggestedRoute { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public bool IsFavorite { get; set; } = false;
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }
}
