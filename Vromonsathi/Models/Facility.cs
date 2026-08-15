namespace Vromonsathi.Models
{
    public class Facility
    {
        public int Id { get; set; }
        public FacilityType Key { get; set; }
        public string NameEn { get; set; } = string.Empty;
        public string? Description { get; set; }

        public decimal DefaultPrice { get; set; }
        public PricingUnit Unit { get; set; }
    }
}