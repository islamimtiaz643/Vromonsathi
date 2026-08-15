using System.ComponentModel.DataAnnotations;

namespace Vromonsathi.Models
{
    public class Destination
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; }

        [MaxLength(150)]
        public string? NameBn { get; set; }

        [Required, MaxLength(100)]
        public string District { get; set; }

        [Required, MaxLength(100)]
        public string Division { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; }

        public string? Description { get; set; }

        public decimal EntryFee { get; set; }

        [MaxLength(100)]
        public string? BestTimeToVisit { get; set; }

        public string? ImageUrl { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [MaxLength(150)]
        public string? NearestViewpointName { get; set; }

        public double? NearestViewpointDistanceKm { get; set; }

        public bool RequiresConvoyEscort { get; set; } = false;

        public bool IsApproved { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Checkpoint> Checkpoints { get; set; } = new List<Checkpoint>();
        public ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();
        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
    }
}