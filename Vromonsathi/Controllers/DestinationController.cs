using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vromonsathi.Data;
using Vromonsathi.Filters;
using Vromonsathi.Models;
using Vromonsathi.Services;
using Vromonsathi.ViewModels;

namespace Vromonsathi.Controllers
{
    public class DestinationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBudgetCalculatorService _budget;

        public DestinationController(ApplicationDbContext context, IBudgetCalculatorService budget)
        {
            _context = context;
            _budget = budget;
        }

        public async Task<IActionResult> Index(string? search, string? district, string? category)
        {
            var query = _context.Destinations.Where(d => d.IsApproved).Include(d => d.Checkpoints).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                query = query.Where(d => d.Name.ToLower().Contains(term) || d.District.ToLower().Contains(term));
            }

            if (!string.IsNullOrEmpty(district))
                query = query.Where(d => d.District == district);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(d => d.Category == category);

            ViewBag.Districts = Vromonsathi.Helpers.BangladeshData.Districts;
            ViewBag.Categories = Vromonsathi.Helpers.BangladeshData.Categories;

            var result = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
            return View(result);
        }

        public async Task<IActionResult> Details(int id)
        {
            var destination = await _context.Destinations
                .Include(d => d.Reviews.Where(r => r.IsApproved))
                .ThenInclude(r => r.TouristUser)
                .Include(d => d.Checkpoints)
                .Include(d => d.Listings.Where(l => l.IsActive))
                .ThenInclude(l => l.VendorProfile)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (destination == null) return NotFound();

            BudgetEstimateResult? sampleEstimate = null;
            List<GroupCostingRow> groupCosting = new();

            var facilitiesExist = await _context.Facilities.AnyAsync();
            if (facilitiesExist)
            {
                sampleEstimate = await _budget.EstimateAsync(new BudgetEstimateRequest
                {
                    DestinationId = id,
                    Days = 3,
                    GroupSize = 4,
                    SelectedFacilities = new List<string> { "Hotel", "Guide" }
                });

                groupCosting = await _budget.BuildGroupCostingTableAsync(id, 3, new List<string> { "Hotel", "Guide" }, new[] { 2, 4, 8, 12 });
            }

            var vm = new DestinationDetailsViewModel
            {
                Destination = destination,
                SampleEstimate = sampleEstimate,
                GroupCosting = groupCosting,
                Listings = destination.Listings.ToList()
            };

            return View(vm);
        }

        [CustomAuthorize("Tourist")]
        [HttpPost]
        public async Task<IActionResult> AddReview(int destinationId, int rating, string comment)
        {
            int userId = HttpContext.Session.GetInt32("UserId")!.Value;

            var review = new Review
            {
                TouristUserId = userId,
                DestinationId = destinationId,
                Rating = rating,
                Comment = comment,
                IsApproved = true
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = destinationId });
        }
    }
}