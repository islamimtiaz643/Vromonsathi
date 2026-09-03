using System.ComponentModel.DataAnnotations;

namespace Vromonsathi.Models
{
    public class VendorPackageOffer
    {
        public int Id { get; set; }

        public int TourPackageId { get; set; }
        public TourPackage? TourPackage { get; set; }

        public int VendorProfileId { get; set; }
        public VendorProfile? VendorProfile { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        [MaxLength(30)]
        public string? Category { get; set; }

        // "Pending", "Approved", "Rejected"
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<BookingAddOn> BookingAddOns { get; set; } = new List<BookingAddOn>();
    }
}