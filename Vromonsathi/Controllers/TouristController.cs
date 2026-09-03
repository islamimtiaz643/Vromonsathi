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
        public async Task<IActionResult> MyWallet()
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            ViewBag.WalletBalance = user!.WalletBalance;

            var history = await _context.Bookings
                .Include(b => b.TourPackage)
                .Include(b => b.Listing)
                .Where(b => b.TouristUserId == CurrentUserId && (b.WalletCreditEarned > 0 || b.WalletCreditUsed > 0))
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(history);
        }

        // ---------- BOOKING ----------
        [HttpGet]
        public async Task<IActionResult> RequestEdit(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.TourPackage).ThenInclude(p => p!.VendorOffers).ThenInclude(o => o.VendorProfile)
                .Include(b => b.AddOns)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.TouristUserId == CurrentUserId);

            if (booking == null || booking.TourPackageId == null) return NotFound();

            var alreadyChosenIds = booking.AddOns.Select(a => a.VendorPackageOfferId).ToHashSet();
            ViewBag.AvailableOffers = booking.TourPackage!.VendorOffers
                .Where(o => o.Status == "Approved" && o.IsActive && !alreadyChosenIds.Contains(o.Id))
                .ToList();

            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> RequestEdit(int bookingId, int[]? requestedOfferIds, string? note)
        {
            var booking = await _context.Bookings
                .Include(b => b.TourPackage)
                .Include(b => b.TouristUser)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.TouristUserId == CurrentUserId);

            if (booking == null) return NotFound();

            var offerTitles = "";
            if (requestedOfferIds != null && requestedOfferIds.Length > 0)
            {
                var offers = await _context.VendorPackageOffers
                    .Where(o => requestedOfferIds.Contains(o.Id))
                    .ToListAsync();
                offerTitles = string.Join(", ", offers.Select(o => o.Title));
            }

            booking.EditRequested = true;
            booking.EditRequestNote = string.IsNullOrWhiteSpace(offerTitles)
                ? note
                : $"Requested add-ons: {offerTitles}. {note}".Trim();

            var admins = await _context.Users.Where(u => u.Role == "Admin").ToListAsync();
            foreach (var admin in admins)
            {
                Vromonsathi.Helpers.NotificationHelper.AddNotification(
                    _context, admin.Id,
                    "Booking edit requested",
                    $"{booking.TouristUser!.FullName} wants to add facilities to their '{booking.TourPackage!.Title}' booking.",
                    "/Admin/PackageBookings");
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Edit request sent to admin. You'll be notified once reviewed.";
            return RedirectToAction("MyBookings");
        }
        [HttpGet]
        public async Task<IActionResult> PayAdvance(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.TourPackage)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.TouristUserId == CurrentUserId);

            if (booking == null) return NotFound();
            if (booking.AdvancePaid)
            {
                TempData["Message"] = "Advance already paid for this booking.";
                return RedirectToAction("MyBookings");
            }

            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> PayAdvance(Vromonsathi.ViewModels.AdvancePaymentViewModel model)
        {
            var booking = await _context.Bookings
                .Include(b => b.TourPackage)
                .Include(b => b.TouristUser)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId && b.TouristUserId == CurrentUserId);

            if (booking == null) return NotFound();

            if (!ModelState.IsValid)
                return View("PayAdvance", booking);

            // SIMULATED payment gateway — no real bKash/Nagad/card integration.
            // In production this would call the provider's API and wait for a callback.
            booking.AdvancePaid = true;
            booking.Status = "Confirmed";

            _context.WalletTransactions.Add(new WalletTransaction
            {
                UserId = CurrentUserId,
                Amount = booking.RequiredAdvance,
                Type = "BookingAdvance",
                PaymentMethod = model.PaymentMethod,
                PhoneNumber = model.PhoneNumber,
                Status = "Completed",
                BookingId = booking.Id,
                ReceiptNote = $"Advance payment for '{booking.TourPackage!.Title}' ({booking.NumberOfPeople} people)"
            });

            var admins = await _context.Users.Where(u => u.Role == "Admin").ToListAsync();
            foreach (var admin in admins)
            {
                Vromonsathi.Helpers.NotificationHelper.AddNotification(
                    _context, admin.Id,
                    "Advance payment received",
                    $"{booking.TouristUser!.FullName} paid ৳{booking.RequiredAdvance:N0} advance for '{booking.TourPackage!.Title}'.",
                    "/Admin/PackageBookings");
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = "Payment received. Your booking is confirmed.";
            return RedirectToAction("Receipt", new { bookingId = booking.Id });
        }

        public async Task<IActionResult> Receipt(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.TourPackage)
                .Include(b => b.TouristUser)
                .Include(b => b.AddOns).ThenInclude(a => a.VendorPackageOffer)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.TouristUserId == CurrentUserId);

            if (booking == null) return NotFound();

            var transaction = await _context.WalletTransactions
                .Where(t => t.BookingId == bookingId && t.Type == "BookingAdvance")
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            ViewBag.Transaction = transaction;
            return View(booking);
        }
        [HttpGet]
        public IActionResult Deposit()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Deposit(Vromonsathi.ViewModels.DepositViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            // SIMULATED payment gateway — no real bKash/Nagad/card integration.
            user.WalletBalance += model.Amount;

            _context.WalletTransactions.Add(new WalletTransaction
            {
                UserId = CurrentUserId,
                Amount = model.Amount,
                Type = "Deposit",
                PaymentMethod = model.PaymentMethod,
                PhoneNumber = model.PhoneNumber,
                Status = "Completed",
                ReceiptNote = $"Wallet top-up via {model.PaymentMethod}"
            });

            await _context.SaveChangesAsync();

            TempData["Message"] = $"৳{model.Amount:N0} added to your wallet. Wallet balance can only be used within Vromonsathi and is not withdrawable.";
            return RedirectToAction("MyWallet");
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

            var alreadyBooked = await Vromonsathi.Helpers.BookingHelper.GetBookedSlotsAsync(_context, packageId);
            if (alreadyBooked + numberOfPeople > package.MaxGroupSize)
            {
                var remaining = Math.Max(package.MaxGroupSize - alreadyBooked, 0);
                TempData["Message"] = $"Only {remaining} spot(s) left on this package. Please reduce your group size.";
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
                WalletCreditEarned = totalUnspent,
                RequiredAdvance = 1500m * numberOfPeople,
                AdvancePaid = false
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

            TempData["Message"] = $"Booking submitted. A ৳{booking.RequiredAdvance:N0} advance payment is required to confirm your spot.";

            return RedirectToAction("PayAdvance", new { bookingId = booking.Id });
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