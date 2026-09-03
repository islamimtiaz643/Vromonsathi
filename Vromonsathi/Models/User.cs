using System.ComponentModel.DataAnnotations;

namespace Vromonsathi.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; }

        [Required, MaxLength(150)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string PasswordSalt { get; set; }

        // "Admin", "Vendor", "Tourist"
        [Required, MaxLength(20)]
        public string Role { get; set; }

        
        public bool IsActive { get; set; } = true;

        public decimal WalletBalance { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public VendorProfile? VendorProfile { get; set; }
    }
}