using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class Content
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        [Required]
        [MaxLength(10)]
        public string ContentType { get; set; }

        public string VideoUrl { get; set; }

        public string BannerImageUrl { get; set; }

        public string BannerText { get; set; }

        public int? Duration { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

    }
}
