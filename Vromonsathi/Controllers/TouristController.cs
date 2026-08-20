using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vromonsathi.Data;
using Vromonsathi.Filters;
using Vromonsathi.Models;

namespace Vromonsathi.Controllers
{
    [CustomAuthorize("Tourist")]
    public class TouristController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TouristController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => HttpContext.Session.GetInt32("UserId")!.Value;

        public async Task<IActionResult> Dashboard()
        {
            var bookings = await _context.Bookings
                .Where(b => b.TouristUserId == CurrentUserId)
                .ToListAsync();

            ViewBag.TotalBookings = bookings.Count;
            ViewBag.PendingBookings = bookings.Count(b => b.Status == "Pending");
            ViewBag.ConfirmedBookings = bookings.Count(b => b.Status == "Confirmed");
            ViewBag.CompletedBookings = bookings.Count(b => b.Status == "Completed");

            var recentBookings = await _context.Bookings
                .Include(b => b.Listing)
                .Where(b => b.TouristUserId == CurrentUserId)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(recentBookings);
        }

        // ---------- BOOKING ----------
        [HttpGet]
        public async Task<IActionResult> BookPackage(int id)
        {
            var package = await _context.TourPackages
                .Include(p => p.Destination)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (package == null) return NotFound();
            return View(package);
        }

        [HttpPost]
        public async Task<IActionResult> BookPackage(int packageId, DateTime startDate, int numberOfPeople)
        {
            var package = await _context.TourPackages.FirstOrDefaultAsync(p => p.Id == packageId && p.IsActive);
            if (package == null) return NotFound();

            if (numberOfPeople < 1) numberOfPeople = 1;

            var booking = new Booking
            {
                TouristUserId = CurrentUserId,
                TourPackageId = packageId,
                StartDate = startDate,
                EndDate = startDate.AddDays(package.DurationDays),
                NumberOfPeople = numberOfPeople,
                TotalPrice = package.Price * numberOfPeople,
                Status = "Pending"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Package booking request submitted.";
            return RedirectToAction("MyBookings");
        }

        // ---------- MY BOOKINGS ----------
        
        public async Task<IActionResult> MyBookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Listing).ThenInclude(l => l.VendorProfile)
                .Include(b => b.TourPackage)
                .Where(b => b.TouristUserId == CurrentUserId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        public async Task<IActionResult> CancelBooking(int id)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == id && b.TouristUserId == CurrentUserId);

            if (booking != null && booking.Status == "Pending")
            {
                booking.Status = "Cancelled";
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MyBookings");
        }

        // ---------- REVIEW A LISTING ----------
        [HttpPost]
        public async Task<IActionResult> AddListingReview(int listingId, int rating, string comment)
        {
            var review = new Review
            {
                TouristUserId = CurrentUserId,
                ListingId = listingId,
                Rating = rating,
                Comment = comment,
                IsApproved = true
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("MyBookings");
        }
    }
}