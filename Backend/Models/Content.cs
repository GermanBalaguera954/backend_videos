using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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
        public string ContentType { get; set; }
        public string VideoUrl { get; set; }
        public string BannerImageUrl { get; set; }
        public string BannerText { get; set; }
        public int DurationInSeconds { get; set; }
        [JsonIgnore]
        public ICollection<Schedule> Schedules { get; set; }
    }
}
