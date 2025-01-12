using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class ScheduleDto
    {
        [Required(ErrorMessage = "El ID del contenido es obligatorio.")]
        public int ContentId { get; set; }

        [Required(ErrorMessage = "La hora de inicio es obligatoria.")]
        public DateTime StartTime { get; set; }

        public int? Duration { get; set; }
    }
}
