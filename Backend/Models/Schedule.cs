using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class Schedule
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int ContentId { get; set; }
        public Content Content { get; set; }
        [Required]
        public DateTime ScheduledAt { get; set; }
        public int DurationInSeconds { get; set; }
        [Required]
        public bool IsActive { get; set; } = true;
        public int? UserId { get; set; }
        public User User { get; set; }
    }
}
