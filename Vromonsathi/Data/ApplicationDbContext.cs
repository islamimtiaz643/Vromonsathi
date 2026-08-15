using Microsoft.EntityFrameworkCore;
using Vromonsathi.Models;

namespace Vromonsathi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<VendorProfile> VendorProfiles { get; set; }
        public DbSet<Destination> Destinations { get; set; }
        public DbSet<Listing> Listings { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Checkpoint> Checkpoints { get; set; }
        public DbSet<Facility> Facilities { get; set; }
        public DbSet<EmergencyContact> EmergencyContacts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<VendorProfile>()
                .HasOne(v => v.User)
                .WithOne(u => u.VendorProfile)
                .HasForeignKey<VendorProfile>(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.VendorProfile)
                .WithMany(v => v.Listings)
                .HasForeignKey(l => l.VendorProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.TouristUser)
                .WithMany()
                .HasForeignKey(b => b.TouristUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Listing)
                .WithMany(l => l.Bookings)
                .HasForeignKey(b => b.ListingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.TouristUser)
                .WithMany()
                .HasForeignKey(r => r.TouristUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Destination)
                .WithMany(d => d.Reviews)
                .HasForeignKey(r => r.DestinationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                 .HasOne(r => r.Listing)
                 .WithMany(l => l.Reviews)
                 .HasForeignKey(r => r.ListingId)
                 .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Checkpoint>()
                .HasOne(c => c.Destination)
                .WithMany(d => d.Checkpoints)
                .HasForeignKey(c => c.DestinationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmergencyContact>()
                .HasOne(c => c.Destination)
                .WithMany(d => d.EmergencyContacts)
                .HasForeignKey(c => c.DestinationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Destination)
                .WithMany(d => d.Listings)
                .HasForeignKey(l => l.DestinationId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}