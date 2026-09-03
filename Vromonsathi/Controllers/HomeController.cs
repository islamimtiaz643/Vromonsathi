using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vromonsathi.Data;
using Vromonsathi.Models;
using Vromonsathi.Services;
using Vromonsathi.ViewModels;

namespace Vromonsathi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBudgetCalculatorService _budget;

        public HomeController(ApplicationDbContext context, IBudgetCalculatorService budget)
        {
            _context = context;
            _budget = budget;
        }

        public async Task<IActionResult> Index()
        {
            var destinations = await _context.Destinations
                .Where(d => d.IsApproved)
                .Include(d => d.Checkpoints)
                .AsNoTracking()
                .ToListAsync();

            var facilities = await _context.Facilities.AsNoTracking().ToListAsync();

            var routeShowcase = destinations.FirstOrDefault(d => d.RequiresConvoyEscort)
                                 ?? destinations.FirstOrDefault(d => d.Checkpoints.Any());

            var defaultDestination = destinations.FirstOrDefault();

            var groupCosting = defaultDestination != null && facilities.Any()
                ? await _budget.BuildGroupCostingTableAsync(
                    defaultDestination.Id, 3,
                    new List<string> { "Hotel", "Guide" },
                    new[] { 2, 4, 8, 12 })
                : new List<GroupCostingRow>();

            var emergencyContacts = await _context.EmergencyContacts
                .Where(c => c.DestinationId == null)
                .AsNoTracking()
                .ToListAsync();

            var announcements = await _context.Announcements
    .OrderByDescending(a => a.CreatedAt)
    .Take(3)
    .ToListAsync();

            var featuredPackagesRaw = await _context.TourPackages
     .Include(p => p.Destination)
     .Where(p => p.IsActive)
     .OrderByDescending(p => p.CreatedAt)
     .Take(6)
     .ToListAsync();

            var featuredPackages = new List<Vromonsathi.Models.TourPackage>();
            foreach (var pkg in featuredPackagesRaw)
            {
                var booked = await Vromonsathi.Helpers.BookingHelper.GetBookedSlotsAsync(_context, pkg.Id);
                pkg.SlotsRemainingComputed = Math.Max(pkg.MaxGroupSize - booked, 0);
                featuredPackages.Add(pkg);
            }

            var vm = new HomeIndexViewModel
            {
                Destinations = destinations,
                Facilities = facilities,
                RouteShowcase = routeShowcase,
                GroupCosting = groupCosting,
                EmergencyContacts = emergencyContacts,
                Announcements = announcements,
                FeaturedPackages = featuredPackages
            };

            return View(vm);
        }

        public IActionResult Terms() => View();
        public IActionResult Privacy() => View();
        public IActionResult Error() => View();
    }
}