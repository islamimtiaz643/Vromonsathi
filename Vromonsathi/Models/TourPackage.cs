using System.ComponentModel.DataAnnotations;

namespace Vromonsathi.Models
{
    public class TourPackage
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; }

        public string? Description { get; set; }

        public int? DestinationId { get; set; }
        public Destination? Destination { get; set; }

        [Required]
        public decimal Price { get; set; }

        public int DurationDays { get; set; } = 3;
        

        public int MaxGroupSize { get; set; } = 15;

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

       
     
       
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<PackageLineItem> LineItems { get; set; } = new List<PackageLineItem>();
        public ICollection<VendorPackageOffer> VendorOffers { get; set; } = new List<VendorPackageOffer>();
    }

}