using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Vromonsathi.Models
{
    public class VendorProfile
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required, MaxLength(150)]
        public string BusinessName { get; set; }

        // "Hotel", "TourGuide", "Transport", "Restaurant"
        [Required, MaxLength(30)]
        public string BusinessType { get; set; }

        public string? Description { get; set; }

        [MaxLength(20)]
        public string? ContactNumber { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        public bool IsApproved { get; set; } = false;
        

        public decimal WalletBalance { get; set; } = 0;


        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
    }
}