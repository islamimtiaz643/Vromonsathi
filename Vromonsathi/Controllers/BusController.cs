using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vromonsathi.Data;

namespace Vromonsathi.Controllers
{
    public class BusController : Controller
    {
        private readonly ApplicationDbContext _context;
        public BusController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index(string? origin, string? destination)
        {
            var query = _context.BusRoutes
                .Include(r => r.VendorProfile)
                .Include(r => r.Destination)
                .Where(r => r.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(origin))
                query = query.Where(r => r.OriginCity.ToLower().Contains(origin.Trim().ToLower()));

            if (!string.IsNullOrWhiteSpace(destination))
                query = query.Where(r => r.DestinationCity.ToLower().Contains(destination.Trim().ToLower()));

            var routes = await query.OrderBy(r => r.DepartureTime).ToListAsync();
            return View(routes);
        }

        public async Task<IActionResult> Details(int id, DateTime? date)
        {
            var route = await _context.BusRoutes
                .Include(r => r.VendorProfile)
                .Include(r => r.Destination)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (route == null) return NotFound();

            var travelDate = (date ?? DateTime.Today).Date;

            var bookedSeats = await _context.BusBookings
                .Where(b => b.BusRouteId == id && b.TravelDate.Date == travelDate && b.Status != "Cancelled")
                .Select(b => b.SeatNumbers)
                .ToListAsync();

            var takenSeats = bookedSeats
                .SelectMany(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(s => s.Trim())
                .ToHashSet();

            ViewBag.TravelDate = travelDate;
            ViewBag.TakenSeats = takenSeats;

            return View(route);
        }
    }
}