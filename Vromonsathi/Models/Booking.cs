using System.ComponentModel.DataAnnotations;

namespace Vromonsathi.Models
{
    public class Booking
    {
        public int Id { get; set; }

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

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? CancellationNote { get; set; }

        public decimal WalletCreditUsed { get; set; } = 0;
        public decimal WalletCreditEarned { get; set; } = 0;
       

        public decimal RequiredAdvance { get; set; } = 0;
        public bool AdvancePaid { get; set; } = false;

        public bool EditRequested { get; set; } = false;
        public string? EditRequestNote { get; set; }
        public bool VendorsPaidOut { get; set; } = false;

        public ICollection<BookingAddOn> AddOns { get; set; } = new List<BookingAddOn>();
    }
}