using System.ComponentModel.DataAnnotations;

namespace Vromonsathi.Models
{
    public class WalletTransaction
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public decimal Amount { get; set; }

        // "Deposit", "BookingAdvance", "FlexibleBudgetCredit", "WalletSpend"
        [MaxLength(30)]
        public string Type { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PaymentMethod { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Completed";

        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }

        public string? ReceiptNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}