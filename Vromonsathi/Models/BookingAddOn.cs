namespace Vromonsathi.Models
{
    public class BookingAddOn
    {
        public int Id { get; set; }

        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        public int VendorPackageOfferId { get; set; }
        public VendorPackageOffer? VendorPackageOffer { get; set; }

        public decimal UnitPrice { get; set; }
    }
}