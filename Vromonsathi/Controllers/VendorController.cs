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
            var bookings = await _context.Bookings.Where(b => listingIds.Contains(b.ListingId)).ToListAsync();

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
            if (!ModelState.IsValid) return View(model);

            existing.Title = model.Title;
            existing.Description = model.Description;
            existing.Price = model.Price;
            existing.Category = model.Category;
            existing.ImageUrl = model.ImageUrl;
            existing.IsActive = model.IsActive;
            existing.DestinationId = model.DestinationId;

            await _context.SaveChangesAsync();
            TempData["Message"] = "Listing updated.";
            return RedirectToAction("Listings");
        }

        public async Task<IActionResult> DeleteListing(int id)
        {
            var vendor = await GetCurrentVendorProfile();
            var listing = await _context.Listings.FirstOrDefaultAsync(l => l.Id == id && l.VendorProfileId == vendor!.Id);
            if (listing != null)
            {
                _context.Listings.Remove(listing);
                await _context.SaveChangesAsync();
            }
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
                .Where(b => listingIds.Contains(b.ListingId))
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        public async Task<IActionResult> UpdateBookingStatus(int id, string status)
        {
            var vendor = await GetCurrentVendorProfile();
            var listingIds = vendor!.Listings.Select(l => l.Id).ToList();

            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id && listingIds.Contains(b.ListingId));
            if (booking != null)
            {
                booking.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Bookings");
        }
    }
}