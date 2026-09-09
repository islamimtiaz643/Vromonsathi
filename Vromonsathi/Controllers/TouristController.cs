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

            var history = await _context.WalletTransactions
                .Where(t => t.UserId == CurrentUserId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(history);
        }

        // ---------- BOOKING: VENDOR LISTINGS (Room/Transport/OtherFacility) ----------
        [HttpGet]
        public async Task<IActionResult> BookListing(int id)
        {
            var listing = await _context.Listings
                .Include(l => l.VendorProfile)
                .FirstOrDefaultAsync(l => l.Id == id && l.IsActive);

            if (listing == null) return NotFound();
            return View(listing);
        }

        [HttpPost]
        public async Task<IActionResult> BookListing(int listingId, DateTime startDate, DateTime? endDate, int numberOfPeople)
        {
            var listing = await _context.Listings
                .Include(l => l.VendorProfile)
                .FirstOrDefaultAsync(l => l.Id == listingId && l.IsActive);
            if (listing == null) return NotFound();

            if (numberOfPeople < 1) numberOfPeople = 1;

            var booking = new Booking
            {
                TouristUserId = CurrentUserId,
                ListingId = listingId,
                StartDate = startDate,
                EndDate = endDate,
                NumberOfPeople = numberOfPeople,
                TotalPrice = listing.Price * numberOfPeople,
                Status = "Pending"
            };

            _context.Bookings.Add(booking);

            Vromonsathi.Helpers.NotificationHelper.AddNotification(
                _context, listing.VendorProfile.UserId,
                "New booking request",
                $"{HttpContext.Session.GetString("FullName")} requested to book '{listing.Title}'.",
                "/Vendor/Bookings");

            await _context.SaveChangesAsync();

            TempData["Message"] = "Booking request submitted. Waiting for vendor confirmation.";
            return RedirectToAction("MyBookings");
        }
        [HttpPost]
        public async Task<IActionResult> BookBus(int busRouteId, DateTime travelDate, string seatNumbers)
        {
            var route = await _context.BusRoutes
                .Include(r => r.VendorProfile)
                .FirstOrDefaultAsync(r => r.Id == busRouteId && r.IsActive);

            if (route == null) return NotFound();
            if (string.IsNullOrWhiteSpace(seatNumbers))
            {
                TempData["Message"] = "Please select at least one seat.";
                return RedirectToAction("Details", "Bus", new { id = busRouteId, date = travelDate.ToString("yyyy-MM-dd") });
            }

            var requestedSeats = seatNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

            var alreadyBookedSeats = await _context.BusBookings
                .Where(b => b.BusRouteId == busRouteId && b.TravelDate.Date == travelDate.Date && b.Status != "Cancelled")
                .Select(b => b.SeatNumbers)
                .ToListAsync();

            var takenSeats = alreadyBookedSeats
                .SelectMany(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(s => s.Trim())
                .ToHashSet();

            if (requestedSeats.Any(s => takenSeats.Contains(s)))
            {
                TempData["Message"] = "One or more selected seats were just booked by someone else. Please pick different seats.";
                return RedirectToAction("Details", "Bus", new { id = busRouteId, date = travelDate.ToString("yyyy-MM-dd") });
            }

            var totalPrice = route.PricePerSeat * requestedSeats.Count;
            var user = await _context.Users.FindAsync(CurrentUserId);

            if (!Vromonsathi.Helpers.WalletHelper.HasSufficientBalance(user!, totalPrice))
            {
                var shortfall = totalPrice - user!.WalletBalance;
                TempData["Message"] = $"Your wallet balance isn't enough for {requestedSeats.Count} seat(s) (৳{totalPrice:N0}). Please recharge at least ৳{shortfall:N0}.";
                return RedirectToAction("RechargeRequired", new { amountNeeded = shortfall, packageId = 0 });
            }

            var booking = new BusBooking
            {
                BusRouteId = busRouteId,
                TouristUserId = CurrentUserId,
                TravelDate = travelDate.Date,
                SeatNumbers = string.Join(",", requestedSeats),
                SeatCount = requestedSeats.Count,
                TotalPrice = totalPrice,
                Status = "Confirmed"
            };

            _context.BusBookings.Add(booking);
            await _context.SaveChangesAsync();

            Vromonsathi.Helpers.WalletHelper.Debit(
                _context, user!, totalPrice, "BusTicket", null,
                $"{requestedSeats.Count} seat(s) on {route.OriginCity} → {route.DestinationCity} ({route.BusName}), {travelDate:dd MMM yyyy}");

            route.VendorProfile!.WalletBalance += totalPrice;

            Vromonsathi.Helpers.NotificationHelper.AddNotification(
                _context, route.VendorProfile.UserId,
                "New bus booking",
                $"{user!.FullName} booked {requestedSeats.Count} seat(s) on {route.OriginCity} → {route.DestinationCity} for {travelDate:dd MMM yyyy}. ৳{totalPrice:N0} credited to your wallet.",
                "/Vendor/BusRoutes");

            await _context.SaveChangesAsync();

            TempData["Message"] = $"Booking confirmed! Seats {booking.SeatNumbers} on {route.OriginCity} → {route.DestinationCity}, {travelDate:dd MMM yyyy}. ৳{totalPrice:N0} paid from your wallet.";
            return RedirectToAction("MyBusBookings");
        }

        public async Task<IActionResult> MyBusBookings()
        {
            var bookings = await _context.BusBookings
                .Include(b => b.BusRoute)
                .Where(b => b.TouristUserId == CurrentUserId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }
        // ---------- BOOKING: TOUR PACKAGES (wallet-gated advance) ----------
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

            var bookedSlots = await Vromonsathi.Helpers.BookingHelper.GetBookedSlotsAsync(_context, id);
            ViewBag.SlotsRemaining = Math.Max(package.MaxGroupSize - bookedSlots, 0);

            return View(package);
        }

        [HttpPost]
        public async Task<IActionResult> BookPackage(int packageId, DateTime startDate, int numberOfPeople, int[]? selectedOfferIds)
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

            // Real invoice = mandatory cost + chosen add-ons only. The unused flexible
            // budget is never charged, so there's nothing to "refund" to the wallet.
            var actualChargePerPerson = mandatoryTotal + addOnCostPerPerson;
            var grossTotal = actualChargePerPerson * numberOfPeople;
            var requiredAdvance = Math.Min(1500m * numberOfPeople, grossTotal);
            var dueAmount = grossTotal - requiredAdvance;
            var dueDate = startDate.AddHours(-48);

            if (!Vromonsathi.Helpers.WalletHelper.HasSufficientBalance(user!, requiredAdvance))
            {
                var shortfall = requiredAdvance - user!.WalletBalance;
                TempData["Message"] = $"Your wallet balance (৳{user.WalletBalance:N0}) isn't enough to cover the ৳{requiredAdvance:N0} advance for {numberOfPeople} people. Please recharge at least ৳{shortfall:N0}, then come back and book again.";
                return RedirectToAction("RechargeRequired", new { amountNeeded = shortfall, packageId });
            }

            var booking = new Booking
            {
                TouristUserId = CurrentUserId,
                TourPackageId = packageId,
                StartDate = startDate,
                EndDate = startDate.AddDays(package.DurationDays),
                NumberOfPeople = numberOfPeople,
                TotalPrice = grossTotal,
                Status = "Confirmed",
                RequiredAdvance = requiredAdvance,
                AdvancePaid = true,
                DueAmount = dueAmount,
                DueDate = dueDate,
                DuePaid = false,
                WalletCreditEarned = 0
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            Vromonsathi.Helpers.WalletHelper.Debit(
     _context, user!, requiredAdvance, "BookingAdvance", booking.Id,
     $"Advance for '{package.Title}' ({numberOfPeople} people)");

            booking.WalletCreditUsed = requiredAdvance;

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
                    $"{HttpContext.Session.GetString("FullName")} booked '{package.Title}' for {numberOfPeople} people. Advance paid from wallet.",
                    "/Admin/PackageBookings");
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = dueAmount > 0
                ? $"Booking confirmed. ৳{requiredAdvance:N0} advance paid from your wallet. Remaining ৳{dueAmount:N0} is due by {dueDate:dd MMM yyyy, h:mm tt} (48 hours before your trip)."
                : "Booking confirmed. Advance paid from your wallet.";

            return RedirectToAction("Receipt", new { bookingId = booking.Id });
        }

        public IActionResult RechargeRequired(decimal amountNeeded, int packageId)
        {
            ViewBag.AmountNeeded = amountNeeded;
            ViewBag.PackageId = packageId;
            return View();
        }

        // ---------- DUE AMOUNT SETTLEMENT (wallet-gated) ----------
        [HttpGet]
        public async Task<IActionResult> PayDue(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.TourPackage)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.TouristUserId == CurrentUserId);

            if (booking == null || booking.TourPackageId == null) return NotFound();
            if (booking.DuePaid || booking.DueAmount <= 0)
            {
                TempData["Message"] = "No due amount remaining on this booking.";
                return RedirectToAction("MyBookings");
            }

            var user = await _context.Users.FindAsync(CurrentUserId);
            ViewBag.WalletBalance = user!.WalletBalance;

            return View(booking);
        }

        [HttpPost]
        [ActionName("PayDue")]
        public async Task<IActionResult> PayDueConfirm(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.TourPackage)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.TouristUserId == CurrentUserId);

            if (booking == null) return NotFound();
            if (booking.DuePaid || booking.DueAmount <= 0)
            {
                TempData["Message"] = "No due amount remaining on this booking.";
                return RedirectToAction("MyBookings");
            }

            var user = await _context.Users.FindAsync(CurrentUserId);

            if (!Vromonsathi.Helpers.WalletHelper.HasSufficientBalance(user!, booking.DueAmount))
            {
                var shortfall = booking.DueAmount - user!.WalletBalance;
                TempData["Message"] = $"Your wallet balance isn't enough to cover the ৳{booking.DueAmount:N0} due amount. Please recharge at least ৳{shortfall:N0}.";
                return RedirectToAction("RechargeRequired", new { amountNeeded = shortfall, packageId = booking.TourPackageId });
            }

            Vromonsathi.Helpers.WalletHelper.Debit(
                _context, user!, booking.DueAmount, "DueSettlement", booking.Id,
                $"Due settlement for '{booking.TourPackage!.Title}'");

            booking.WalletCreditUsed += booking.DueAmount;
            booking.DuePaid = true;

            var admins = await _context.Users.Where(u => u.Role == "Admin").ToListAsync();
            foreach (var admin in admins)
            {
                Vromonsathi.Helpers.NotificationHelper.AddNotification(
                    _context, admin.Id,
                    "Due amount settled",
                    $"{HttpContext.Session.GetString("FullName")} paid the remaining ৳{booking.DueAmount:N0} due for '{booking.TourPackage!.Title}'.",
                    "/Admin/PackageBookings");
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = "Due amount paid from your wallet. Your booking is fully settled.";
            return RedirectToAction("Receipt", new { bookingId = booking.Id });
        }

        // ---------- BOOKING EDIT REQUEST ----------
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

        // ---------- WALLET DEPOSIT (simulated) ----------
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
            Vromonsathi.Helpers.WalletHelper.Credit(
                _context, user, model.Amount, "Deposit", null,
                $"Wallet top-up via {model.PaymentMethod} ({model.PhoneNumber})");

            await _context.SaveChangesAsync();

            TempData["Message"] = $"৳{model.Amount:N0} added to your wallet. Wallet balance can only be used within Vromonsathi and is not withdrawable.";
            return RedirectToAction("MyWallet");
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