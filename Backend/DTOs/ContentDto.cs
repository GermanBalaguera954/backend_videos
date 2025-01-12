using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class ContentDto
    {
        [Required(ErrorMessage = "El título es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El título no puede tener más de 100 caracteres.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "El tipo de contenido es obligatorio.")]
        [MaxLength(10, ErrorMessage = "El tipo de contenido no puede tener más de 10 caracteres.")]
        public string ContentType { get; set; }

        public string VideoUrl { get; set; }

        public string BannerImageUrl { get; set; }
        
        public string BannerText { get; set; }

        public int Duration { get; set; }
    }
}
