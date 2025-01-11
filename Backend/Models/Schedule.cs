using System;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class Schedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ContentId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public Content Content { get; set; }

    }
}
