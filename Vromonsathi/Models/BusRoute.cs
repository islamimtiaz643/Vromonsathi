using System.ComponentModel.DataAnnotations;

namespace Vromonsathi.Models
{
    public class BusRoute
    {
        public int Id { get; set; }

        public int VendorProfileId { get; set; }
        public VendorProfile? VendorProfile { get; set; }

        public int? DestinationId { get; set; }
        public Destination? Destination { get; set; }

        [Required, MaxLength(100)]
        public string OriginCity { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string DestinationCity { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string BusName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string BusType { get; set; } = "Non-AC"; // "AC", "Non-AC", "Sleeper"

        [Required, MaxLength(20)]
        public string DepartureTime { get; set; } = "22:00"; // stored as "HH:mm"

        [Required]
        public decimal PricePerSeat { get; set; }

        [Required]
        public int TotalSeats { get; set; } = 32;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<BusBooking> Bookings { get; set; } = new List<BusBooking>();
    }
}