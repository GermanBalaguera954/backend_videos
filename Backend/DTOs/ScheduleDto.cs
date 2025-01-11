using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class ScheduleDto
    {
        [Required]
        public int ContentId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

    }
}
