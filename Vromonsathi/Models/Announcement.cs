using System.ComponentModel.DataAnnotations;

namespace Vromonsathi.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}