using System.ComponentModel.DataAnnotations;

namespace Vromonsathi.Models
{
    public class Listing
    {
        public int Id { get; set; }

        [Required]
        public int VendorProfileId { get; set; }
        public VendorProfile VendorProfile { get; set; }

        public int? DestinationId { get; set; }
        public Destination? Destination { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        [MaxLength(30)]
        public string Category { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}