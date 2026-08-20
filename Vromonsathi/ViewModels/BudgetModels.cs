namespace Vromonsathi.ViewModels
{
    public class BudgetEstimateRequest
    {
        public int DestinationId { get; set; }
        public int Days { get; set; } = 3;
        public int GroupSize { get; set; } = 4;
        public List<string> SelectedFacilities { get; set; } = new() { "Hotel", "Guide" };
    }

    public class BudgetLineItem
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class BudgetEstimateResult
    {
        public decimal TotalCost { get; set; }
        public decimal CostPerPerson { get; set; }
        public List<BudgetLineItem> Breakdown { get; set; } = new();
        public List<Vromonsathi.Models.Checkpoint> RouteCheckpoints { get; set; } = new();
        public string? NearestViewpoint { get; set; }
        public double? ViewpointDistanceKm { get; set; }
    }

    public class GroupCostingRow
    {
        public int GroupSize { get; set; }
        public decimal SharedCost { get; set; }
        public decimal PerPersonFixed { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal CostPerPerson { get; set; }
    }

    public class DestinationDetailsViewModel
    {
        public Vromonsathi.Models.Destination Destination { get; set; } = null!;
        public BudgetEstimateResult? SampleEstimate { get; set; }
        public List<GroupCostingRow> GroupCosting { get; set; } = new();
        public List<Vromonsathi.Models.Listing> Listings { get; set; } = new();
    }

    public class SosRequest
    {
        public int DestinationId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class HomeIndexViewModel
    {
        public List<Vromonsathi.Models.Destination> Destinations { get; set; } = new();
        public List<Vromonsathi.Models.Facility> Facilities { get; set; } = new();
        public Vromonsathi.Models.Destination? RouteShowcase { get; set; }
        public List<GroupCostingRow> GroupCosting { get; set; } = new();
        public List<Vromonsathi.Models.EmergencyContact> EmergencyContacts { get; set; } = new();
        public List<Vromonsathi.Models.Announcement> Announcements { get; set; } = new();
        public List<Vromonsathi.Models.Listing> FeaturedListings { get; set; } = new();
    }
}