using System.ComponentModel.DataAnnotations;

namespace Vromonsathi.ViewModels
{
    public class RegisterViewModel
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; }

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Required, DataType(DataType.Password), MinLength(6)]
        public string Password { get; set; }

        [Required, DataType(DataType.Password), Compare("Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Role { get; set; } // "Tourist" or "Vendor"

        // Only used if Role == Vendor
        public string? BusinessName { get; set; }
        public string? BusinessType { get; set; }
    }
}