using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models
{
    public class Profile
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(UserId))]
        public Guid UserId
        {
            get; set;
        }
        public User User { get; set; } = null!;

        public double HeightCm
        {
            get; set;
        }
        public double TargetWeightKg
        {
            get; set;
        }
        public int ActivityDaysPerWeek
        {
            get; set;
        }

        public DateOnly BirthDate
        {
            get; set;
        }
        public Sex Sex
        {
            get; set;
        }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

public enum Sex
{
    Male,
    Female
}
