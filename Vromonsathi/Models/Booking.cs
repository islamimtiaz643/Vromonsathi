using System.ComponentModel.DataAnnotations;

namespace Vromonsathi.Models
{
    public class Booking
    {
        public int Id { get; set; }[MaxLength(20)]

        [Required]
        public int TouristUserId { get; set; }
        public User TouristUser { get; set; }

        public int? ListingId { get; set; }
        public Listing? Listing { get; set; }

        public int? TourPackageId { get; set; }
        public TourPackage? TourPackage { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int NumberOfPeople { get; set; } = 1;

        public decimal TotalPrice { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public string? CancellationNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}