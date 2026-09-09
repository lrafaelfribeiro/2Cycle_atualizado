using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models
{
    public class RefreshToken
    {
        [Key]
        public Guid Id
        {
            get; set;
        }
        [ForeignKey(nameof(UserId))]
        public Guid UserId
        {
            get; set;
        }
        public User User { get; set; } = null!;
        public Guid FamilyId { get; set; }

        [Required]
        public string HashToken { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);

        public bool Revoked { get; set; } = false;

    }
}
