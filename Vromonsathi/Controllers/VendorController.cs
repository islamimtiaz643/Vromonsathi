using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vromonsathi.Data;
using Vromonsathi.Filters;
using Vromonsathi.Models;

namespace Vromonsathi.Controllers
{
    [CustomAuthorize("Vendor")]
    public class VendorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VendorController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<VendorProfile?> GetCurrentVendorProfile()
        {
            int userId = HttpContext.Session.GetInt32("UserId")!.Value;
            return await _context.VendorProfiles.Include(v => v.Listings)
                .FirstOrDefaultAsync(v => v.UserId == userId);
        }

        public async Task<IActionResult> Dashboard()
        {
            var vendor = await GetCurrentVendorProfile();
            if (vendor == null) return RedirectToAction("Login", "Account");

            ViewBag.VendorProfile = vendor;
            ViewBag.TotalListings = vendor.Listings.Count;

            var listingIds = vendor.Listings.Select(l => l.Id).ToList();
            var bookings = await _context.Bookings
                .Where(b => b.ListingId != null && listingIds.Contains(b.ListingId.Value))
                .ToListAsync();

            ViewBag.TotalBookings = bookings.Count;
            ViewBag.PendingBookings = bookings.Count(b => b.Status == "Pending");
            ViewBag.Revenue = bookings.Where(b => b.Status == "Confirmed" || b.Status == "Completed").Sum(b => b.TotalPrice);

            return View();
        }

        // ---------- LISTINGS ----------
        public async Task<IActionResult> Listings()
        {
            var vendor = await GetCurrentVendorProfile();
            if (vendor == null) return RedirectToAction("Login", "Account");
            return View(vendor.Listings.OrderByDescending(l => l.CreatedAt).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> CreateListing()
        {
            var vendor = await GetCurrentVendorProfile();
            if (vendor == null || !vendor.IsApproved)
            {
                TempData["Message"] = "Your vendor account must be approved by admin before you can add listings.";
                return RedirectToAction("Dashboard");
            }

            ViewBag.Destinations = await _context.Destinations.OrderBy(d => d.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateListing(Listing model)
        {
            var vendor = await GetCurrentVendorProfile();
            if (vendor == null || !vendor.IsApproved) return RedirectToAction("Dashboard");

            ModelState.Remove("VendorProfileId");
            ModelState.Remove("VendorProfile");
            ModelState.Remove("Destination");
            if (!ModelState.IsValid)
            {
                ViewBag.Destinations = await _context.Destinations.OrderBy(d => d.Name).ToListAsync();
                return View(model);
            }

            model.VendorProfileId = vendor.Id;
            _context.Listings.Add(model);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Listing added.";
            return RedirectToAction("Listings");
        }

        [HttpGet]
        public async Task<IActionResult> EditListing(int id)
        {
            var vendor = await GetCurrentVendorProfile();
            var listing = await _context.Listings.FirstOrDefaultAsync(l => l.Id == id && l.VendorProfileId == vendor!.Id);
            if (listing == null) return NotFound();

            ViewBag.Destinations = await _context.Destinations.OrderBy(d => d.Name).ToListAsync();
            return View(listing);
        }

        [HttpPost]
        public async Task<IActionResult> EditListing(Listing model)
        {
            var vendor = await GetCurrentVendorProfile();
            var existing = await _context.Listings.FirstOrDefaultAsync(l => l.Id == model.Id && l.VendorProfileId == vendor!.Id);
            if (existing == null) return NotFound();

            ModelState.Remove("VendorProfileId");
            ModelState.Remove("VendorProfile");
            ModelState.Remove("Destination");
            if (!ModelState.IsValid)
            {
                ViewBag.Destinations = await _context.Destinations.OrderBy(d => d.Name).ToListAsync();
                return View(model);
            }

            existing.Title = model.Title;
            existing.Description = model.Description;
            existing.Price = model.Price;
            existing.Category = model.Category;
            existing.ImageUrl = model.ImageUrl;
            existing.IsActive = model.IsActive;
            existing.DestinationId = model.DestinationId;

            var activeBookingsOnEdit = await _context.Bookings
                .Where(b => b.ListingId == existing.Id && b.Status != "Cancelled" && b.Status != "Completed")
                .ToListAsync();

            foreach (var booking in activeBookingsOnEdit)
            {
                Vromonsathi.Helpers.NotificationHelper.AddNotification(
                    _context, booking.TouristUserId,
                    "Listing updated",
                    $"'{existing.Title}' was updated by the vendor. Please review the latest details.",
                    "/Tourist/MyBookings");
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Listing updated.";
            return RedirectToAction("Listings");
        }

        public async Task<IActionResult> DeleteListing(int id)
        {
            var vendor = await GetCurrentVendorProfile();
            var listing = await _context.Listings.FirstOrDefaultAsync(l => l.Id == id && l.VendorProfileId == vendor!.Id);
            if (listing == null) return RedirectToAction("Listings");

            var admins = await _context.Users.Where(u => u.Role == "Admin").ToListAsync();
            foreach (var admin in admins)
            {
                Vromonsathi.Helpers.NotificationHelper.AddNotification(
                    _context, admin.Id,
                    "Vendor removed a listing",
                    $"{vendor.BusinessName} removed listing '{listing.Title}'.",
                    "/Admin/Vendors");
            }

            var activeBookings = await _context.Bookings
                .Where(b => b.ListingId == id && b.Status != "Cancelled" && b.Status != "Completed")
                .ToListAsync();

            bool hasAnyBookingHistory = await _context.Bookings.AnyAsync(b => b.ListingId == id);

            if (activeBookings.Any())
            {
                foreach (var booking in activeBookings)
                {
                    var note = $"'{listing.Title}' was removed by the vendor. Your payment will be refunded to your original payment method within 3-5 business days.";
                    booking.Status = "Cancelled";
                    booking.CancellationNote = note;

                    Vromonsathi.Helpers.NotificationHelper.AddNotification(
                        _context, booking.TouristUserId,
                        "Booking cancelled — refund pending",
                        note,
                        "/Tourist/MyBookings");
                }

                listing.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Listing removed. {activeBookings.Count} affected booking(s) were cancelled and marked for refund.";
                return RedirectToAction("Listings");
            }

            if (hasAnyBookingHistory)
            {
                listing.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["Message"] = "This listing has past booking history and was deactivated instead of deleted.";
                return RedirectToAction("Listings");
            }

            _context.Listings.Remove(listing);
            await _context.SaveChangesAsync();
            return RedirectToAction("Listings");
        }

        // ---------- BOOKINGS ----------
        public async Task<IActionResult> Bookings()
        {
            var vendor = await GetCurrentVendorProfile();
            if (vendor == null) return RedirectToAction("Login", "Account");

            var listingIds = vendor.Listings.Select(l => l.Id).ToList();
            var bookings = await _context.Bookings
                .Include(b => b.TouristUser)
                .Include(b => b.Listing)
                .Where(b => b.ListingId != null && listingIds.Contains(b.ListingId.Value))
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        public async Task<IActionResult> UpdateBookingStatus(int id, string status)
        {
            var vendor = await GetCurrentVendorProfile();
            var listingIds = vendor!.Listings.Select(l => l.Id).ToList();

            var booking = await _context.Bookings
                .Include(b => b.Listing)
                .FirstOrDefaultAsync(b => b.Id == id && b.ListingId != null && listingIds.Contains(b.ListingId.Value));

            if (booking != null)
            {
                booking.Status = status;

                Vromonsathi.Helpers.NotificationHelper.AddNotification(
                    _context, booking.TouristUserId,
                    $"Booking {status.ToLower()}",
                    $"Your booking for '{booking.Listing!.Title}' is now {status}.",
                    "/Tourist/MyBookings");

                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Bookings");
        }
    
            // ---------- OFFERS ON ADMIN PACKAGES ----------
        public async Task<IActionResult> BrowsePackages()
        {
            var packages = await _context.TourPackages
                .Include(p => p.Destination)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(packages);
        }

        [HttpGet]
        public async Task<IActionResult> CreateOffer(int packageId)
        {
            var vendor = await GetCurrentVendorProfile();
            if (vendor == null || !vendor.IsApproved)
            {
                TempData["Message"] = "Your vendor account must be approved before submitting offers.";
                return RedirectToAction("Dashboard");
            }

            var package = await _context.TourPackages.FindAsync(packageId);
            if (package == null) return NotFound();

            ViewBag.Package = package;
            return View(new VendorPackageOffer { TourPackageId = packageId });
        }

        [HttpPost]
        public async Task<IActionResult> CreateOffer(VendorPackageOffer model)
        {
            var vendor = await GetCurrentVendorProfile();
            if (vendor == null || !vendor.IsApproved) return RedirectToAction("Dashboard");

            ModelState.Remove("TourPackage");
            ModelState.Remove("VendorProfile");
            if (!ModelState.IsValid)
            {
                ViewBag.Package = await _context.TourPackages.FindAsync(model.TourPackageId);
                return View(model);
            }

            model.VendorProfileId = vendor.Id;
            model.Status = "Pending";
            _context.VendorPackageOffers.Add(model);

            var admins = await _context.Users.Where(u => u.Role == "Admin").ToListAsync();
            var package = await _context.TourPackages.FindAsync(model.TourPackageId);
            foreach (var admin in admins)
            {
                Vromonsathi.Helpers.NotificationHelper.AddNotification(
                    _context, admin.Id,
                    "New vendor offer",
                    $"{vendor.BusinessName} offered '{model.Title}' for package '{package!.Title}'.",
                    "/Admin/VendorOffers");
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Offer submitted for admin approval.";
            return RedirectToAction("MyOffers");
        }

        public async Task<IActionResult> MyOffers()
        {
            var vendor = await GetCurrentVendorProfile();
            if (vendor == null) return RedirectToAction("Login", "Account");

            var offers = await _context.VendorPackageOffers
                .Include(o => o.TourPackage)
                .Where(o => o.VendorProfileId == vendor.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(offers);
        }

        public async Task<IActionResult> DeleteOffer(int id)
        {
            var vendor = await GetCurrentVendorProfile();
            var offer = await _context.VendorPackageOffers.FirstOrDefaultAsync(o => o.Id == id && o.VendorProfileId == vendor!.Id);
            if (offer != null)
            {
                _context.VendorPackageOffers.Remove(offer);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("MyOffers");
        }
    }
}
