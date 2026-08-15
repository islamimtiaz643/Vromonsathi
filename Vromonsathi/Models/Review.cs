using System.ComponentModel.DataAnnotations;

namespace Vromonsathi.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int TouristUserId { get; set; }
        public User TouristUser { get; set; }

        public int? DestinationId { get; set; }
        public Destination? Destination { get; set; }

        public int? ListingId { get; set; }
        public Listing? Listing { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        public bool IsApproved { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}