using System.ComponentModel.DataAnnotations;

namespace Vromonsathi.Models
{
    public class PackageLineItem
    {
        public int Id { get; set; }

        public int TourPackageId { get; set; }
        public TourPackage? TourPackage { get; set; }

        [Required, MaxLength(100)]
        public string Label { get; set; } = string.Empty;

        [Required]
        public decimal Cost { get; set; }

        public bool IsMandatory { get; set; } = true;

        [MaxLength(30)]
        public string? Category { get; set; }
    }
}