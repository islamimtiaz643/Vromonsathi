using System.ComponentModel.DataAnnotations;

namespace Vromonsathi.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        public string? LinkUrl { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}