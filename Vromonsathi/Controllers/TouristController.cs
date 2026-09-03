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
            ViewBag.RefundNotices = bookings.Where(b => b.Status == "Cancelled" && b.CancellationNote != null).ToList();
            ViewBag.TotalBookings = bookings.Count;

            var currentUser = await _context.Users.FindAsync(CurrentUserId);
            ViewBag.WalletBalance = currentUser!.WalletBalance;
            ViewBag.PendingBookings = bookings.Count(b => b.Status == "Pending");
            ViewBag.ConfirmedBookings = bookings.Count(b => b.Status == "Confirmed");
            ViewBag.CompletedBookings = bookings.Count(b => b.Status == "Completed");


            var recentBookings = await _context.Bookings
    .Include(b => b.Listing)
    .Include(b => b.TourPackage)
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
                .Include(p => p.LineItems)
                .Include(p => p.VendorOffers.Where(o => o.Status == "Approved" && o.IsActive))
                .ThenInclude(o => o.VendorProfile)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (package == null) return NotFound();

            var user = await _context.Users.FindAsync(CurrentUserId);
            ViewBag.WalletBalance = user!.WalletBalance;

            var mandatoryTotal = package.LineItems.Where(l => l.IsMandatory).Sum(l => l.Cost);
            ViewBag.MandatoryTotal = mandatoryTotal;
            ViewBag.FlexibleBudget = package.Price - mandatoryTotal;

            return View(package);
        }

        [HttpPost]
        public async Task<IActionResult> BookPackage(int packageId, DateTime startDate, int numberOfPeople, int[]? selectedOfferIds, bool useWallet)
        {
            var package = await _context.TourPackages
                .Include(p => p.LineItems)
                .Include(p => p.VendorOffers)
                .FirstOrDefaultAsync(p => p.Id == packageId && p.IsActive);

            if (package == null) return NotFound();
            if (numberOfPeople < 1) numberOfPeople = 1;
            if (numberOfPeople > package.MaxGroupSize)
            {
                TempData["Message"] = $"This package allows a maximum of {package.MaxGroupSize} people.";
                return RedirectToAction("BookPackage", new { id = packageId });
            }

            var user = await _context.Users.FindAsync(CurrentUserId);

            var mandatoryTotal = package.LineItems.Where(l => l.IsMandatory).Sum(l => l.Cost);
            var flexibleBudget = package.Price - mandatoryTotal;

            var chosenOffers = new List<VendorPackageOffer>();
            decimal addOnCostPerPerson = 0;

            if (selectedOfferIds != null && selectedOfferIds.Length > 0)
            {
                chosenOffers = package.VendorOffers
                    .Where(o => selectedOfferIds.Contains(o.Id) && o.Status == "Approved" && o.IsActive)
                    .ToList();
                addOnCostPerPerson = chosenOffers.Sum(o => o.Price);
            }

            if (addOnCostPerPerson > flexibleBudget)
            {
                TempData["Message"] = $"Selected add-ons (৳{addOnCostPerPerson}) exceed your flexible budget (৳{flexibleBudget}) per person. Please deselect some.";
                return RedirectToAction("BookPackage", new { id = packageId });
            }

            var unspentPerPerson = flexibleBudget - addOnCostPerPerson;
            var totalUnspent = unspentPerPerson * numberOfPeople;

            decimal walletUsed = 0;
            var basePrice = package.Price * numberOfPeople;
            var addOnTotal = addOnCostPerPerson * numberOfPeople;
            var grandTotal = basePrice + addOnTotal;

            if (useWallet && user!.WalletBalance > 0)
            {
                walletUsed = Math.Min(user.WalletBalance, grandTotal);
                user.WalletBalance -= walletUsed;
                grandTotal -= walletUsed;
            }

            if (totalUnspent > 0)
            {
                user!.WalletBalance += totalUnspent;
            }

            var booking = new Booking
            {
                TouristUserId = CurrentUserId,
                TourPackageId = packageId,
                StartDate = startDate,
                EndDate = startDate.AddDays(package.DurationDays),
                NumberOfPeople = numberOfPeople,
                TotalPrice = grandTotal,
                Status = "Pending",
                WalletCreditUsed = walletUsed,
                WalletCreditEarned = totalUnspent
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            foreach (var offer in chosenOffers)
            {
                _context.BookingAddOns.Add(new BookingAddOn
                {
                    BookingId = booking.Id,
                    VendorPackageOfferId = offer.Id,
                    UnitPrice = offer.Price
                });
            }

            var admins = await _context.Users.Where(u => u.Role == "Admin").ToListAsync();
            foreach (var admin in admins)
            {
                Vromonsathi.Helpers.NotificationHelper.AddNotification(
                    _context, admin.Id,
                    "New package booking",
                    $"{HttpContext.Session.GetString("FullName")} booked '{package.Title}' for {numberOfPeople} people.",
                    "/Admin/PackageBookings");
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = totalUnspent > 0
                ? $"Booking submitted. ৳{totalUnspent:N0} unused flexible budget was saved to your wallet."
                : "Package booking request submitted.";

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