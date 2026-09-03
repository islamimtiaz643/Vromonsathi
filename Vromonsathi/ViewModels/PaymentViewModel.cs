using System.ComponentModel.DataAnnotations;

namespace Vromonsathi.ViewModels
{
    public class AdvancePaymentViewModel
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = "bKash";

        [Required, RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "Enter a valid 11-digit Bangladeshi mobile number.")]
        public string PhoneNumber { get; set; } = string.Empty;

        // Only used when PaymentMethod == "Card"
        public string? CardLast4 { get; set; }
    }
    public class DepositViewModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Range(100, 100000, ErrorMessage = "Enter an amount between ৳100 and ৳100,000.")]
        public decimal Amount { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string PaymentMethod { get; set; } = "bKash";

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "Enter a valid 11-digit Bangladeshi mobile number.")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? CardLast4 { get; set; }
    }
}