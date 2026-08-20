using Microsoft.EntityFrameworkCore;
using Vromonsathi.Data;
using Vromonsathi.Models;
using Vromonsathi.ViewModels;

namespace Vromonsathi.Services
{
    // Dynamic pricing engine. Pulls real, live vendor Listing prices for the
    // chosen destination wherever available; falls back to the Facility's
    // DefaultPrice only when no vendor has listed that service yet.
    public class BudgetCalculatorService : IBudgetCalculatorService
    {
        private readonly ApplicationDbContext _db;

        public BudgetCalculatorService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<BudgetEstimateResult> EstimateAsync(BudgetEstimateRequest request)
        {
            var destination = await _db.Destinations
                .Include(d => d.Checkpoints)
                .Include(d => d.Listings.Where(l => l.IsActive))
                .FirstOrDefaultAsync(d => d.Id == request.DestinationId)
                ?? throw new ArgumentException("Unknown destination");

            var facilities = await _db.Facilities.ToListAsync();

            var result = new BudgetEstimateResult
            {
                RouteCheckpoints = destination.Checkpoints.OrderBy(c => c.SequenceOrder).ToList(),
                NearestViewpoint = destination.NearestViewpointName,
                ViewpointDistanceKm = destination.NearestViewpointDistanceKm
            };

            int days = Math.Max(request.Days, 1);
            int group = Math.Max(request.GroupSize, 1);
            int nights = Math.Max(days - 1, 1);

            decimal total = 0;

            decimal entryFee = destination.EntryFee * group;
            total += entryFee;
            result.Breakdown.Add(new BudgetLineItem { Label = "Entry & permit fees", Amount = entryFee });

            var cheapestRoom = destination.Listings.Where(l => l.Category == "Room").OrderBy(l => l.Price).FirstOrDefault();
            var cheapestGuide = destination.Listings.Where(l => l.Category == "OtherFacility").OrderBy(l => l.Price).FirstOrDefault();
            var cheapestTransport = destination.Listings.Where(l => l.Category == "TransportService").OrderBy(l => l.Price).FirstOrDefault();

            foreach (var key in request.SelectedFacilities.Distinct())
            {
                var facility = facilities.FirstOrDefault(f => f.Key.ToString() == key);
                if (facility == null) continue;

                decimal cost = key switch
                {
                    "Hotel" => (cheapestRoom?.Price ?? facility.DefaultPrice) * nights * (decimal)Math.Ceiling(group / 2.0),
                    "Guide" => (cheapestGuide?.Price ?? facility.DefaultPrice) * days,
                    "Transport" => (cheapestTransport?.Price ?? facility.DefaultPrice) * days,
                    "Meals" => facility.DefaultPrice * days * group,
                    "Sim" => facility.DefaultPrice * group,
                    "Insurance" => facility.DefaultPrice * group,
                    _ => 0
                };

                total += cost;
                result.Breakdown.Add(new BudgetLineItem { Label = facility.NameEn, Amount = cost });
            }

            result.TotalCost = total;
            result.CostPerPerson = Math.Round(total / group, 0);

            return result;
        }

        public async Task<List<GroupCostingRow>> BuildGroupCostingTableAsync(int destinationId, int days, List<string> facilities, int[] groupSizes)
        {
            var rows = new List<GroupCostingRow>();

            foreach (var size in groupSizes)
            {
                var estimate = await EstimateAsync(new BudgetEstimateRequest
                {
                    DestinationId = destinationId,
                    Days = days,
                    GroupSize = size,
                    SelectedFacilities = facilities
                });

                decimal shared = estimate.Breakdown
                    .Where(b => b.Label.Contains("guide", StringComparison.OrdinalIgnoreCase)
                             || b.Label.Contains("transport", StringComparison.OrdinalIgnoreCase))
                    .Sum(b => b.Amount);

                decimal fixedCost = estimate.TotalCost - shared;

                rows.Add(new GroupCostingRow
                {
                    GroupSize = size,
                    SharedCost = shared,
                    PerPersonFixed = fixedCost,
                    TotalBudget = estimate.TotalCost,
                    CostPerPerson = estimate.CostPerPerson
                });
            }

            return rows;
        }
    }
}