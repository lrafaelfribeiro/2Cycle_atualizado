using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class User
    {
        [Key]
        public Guid Id
        {
            get; set;
        }

        [Required]
        [MaxLength(100)]
        public required string Name
        {
            get; set;
        }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public required string Email
        {
            get; set;
        }

        [Required]
        public string? PasswordHash
        {
            get; set;
        }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Profile? Profile
        {
            get; set;
        }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
