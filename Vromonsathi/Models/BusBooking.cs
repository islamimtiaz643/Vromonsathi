using System.ComponentModel.DataAnnotations;

namespace Vromonsathi.Models
{
    public class BusBooking
    {
        public int Id { get; set; }

        public int BusRouteId { get; set; }
        public BusRoute? BusRoute { get; set; }

        [Required]
        public int TouristUserId { get; set; }
        public User? TouristUser { get; set; }

        [Required]
        public DateTime TravelDate { get; set; }

        // Comma-separated seat codes, e.g. "A1,A2,B3"
        [Required, MaxLength(200)]
        public string SeatNumbers { get; set; } = string.Empty;

        public int SeatCount { get; set; }

        public decimal TotalPrice { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Confirmed"; // "Confirmed", "Cancelled"

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}