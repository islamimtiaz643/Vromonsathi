using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vromonsathi.Data;
using Vromonsathi.Filters;
using Vromonsathi.Models;

namespace Vromonsathi.Controllers
{
    [CustomAuthorize("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<string> SaveUploadedImage(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                throw new InvalidOperationException("Only jpg, jpeg, png, webp images are allowed.");

            var fileName = $"{Guid.NewGuid()}{ext}";
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsPath);
            var fullPath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/uploads/" + fileName;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers = await _context.Users.CountAsync(u => u.Role == "Tourist");
            ViewBag.TotalVendors = await _context.Users.CountAsync(u => u.Role == "Vendor");
            ViewBag.PendingVendors = await _context.VendorProfiles.CountAsync(v => !v.IsApproved);
            ViewBag.TotalDestinations = await _context.Destinations.CountAsync();
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.TotalRevenue = await _context.Bookings
                .Where(b => b.Status == "Confirmed" || b.Status == "Completed")
                .SumAsync(b => (decimal?)b.TotalPrice) ?? 0;

            return View();
        }

        // ---------- USERS ----------
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> ToggleUserActive(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Users");
        }

        // ---------- VENDORS ----------
        public async Task<IActionResult> Vendors()
        {
            var vendors = await _context.VendorProfiles
                .Include(v => v.User)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
            return View(vendors);
        }

        public async Task<IActionResult> ApproveVendor(int id)
        {
            var vendor = await _context.VendorProfiles.FindAsync(id);
            if (vendor != null)
            {
                vendor.IsApproved = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Vendors");
        }

        public async Task<IActionResult> RejectVendor(int id)
        {
            var vendor = await _context.VendorProfiles.FindAsync(id);
            if (vendor != null)
            {
                vendor.IsApproved = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Vendors");
        }

        // ---------- DESTINATIONS ----------
        public async Task<IActionResult> Destinations()
        {
            var list = await _context.Destinations.OrderByDescending(d => d.CreatedAt).ToListAsync();
            return View(list);
        }

        [HttpGet]
        public IActionResult CreateDestination()
        {
            ViewBag.Divisions = Vromonsathi.Helpers.BangladeshData.Divisions;
            ViewBag.Districts = Vromonsathi.Helpers.BangladeshData.Districts;
            ViewBag.Categories = Vromonsathi.Helpers.BangladeshData.Categories;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateDestination(Destination model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(model);

            if (imageFile != null && imageFile.Length > 0)
            {
                model.ImageUrl = await SaveUploadedImage(imageFile);
            }

            _context.Destinations.Add(model);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Destination added.";
            return RedirectToAction("Destinations");
        }

        [HttpGet]
        public async Task<IActionResult> EditDestination(int id)
        {
            var d = await _context.Destinations.FindAsync(id);
            if (d == null) return NotFound();

            ViewBag.Divisions = Vromonsathi.Helpers.BangladeshData.Divisions;
            ViewBag.Districts = Vromonsathi.Helpers.BangladeshData.Districts;
            ViewBag.Categories = Vromonsathi.Helpers.BangladeshData.Categories;
            return View(d);
        }

        [HttpPost]
        public async Task<IActionResult> EditDestination(Destination model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(model);

            if (imageFile != null && imageFile.Length > 0)
            {
                model.ImageUrl = await SaveUploadedImage(imageFile);
            }

            _context.Destinations.Update(model);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Destination updated.";
            return RedirectToAction("Destinations");
        }

        public async Task<IActionResult> DeleteDestination(int id)
        {
            var d = await _context.Destinations.FindAsync(id);
            if (d != null)
            {
                _context.Destinations.Remove(d);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Destinations");
        }

        // ---------- ANNOUNCEMENTS ----------
        public async Task<IActionResult> Announcements()
        {
            var list = await _context.Announcements.OrderByDescending(a => a.CreatedAt).ToListAsync();
            return View(list);
        }

        [HttpGet]
        public IActionResult CreateAnnouncement() => View();

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement(Announcement model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Announcements.Add(model);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Announcement posted.";
            return RedirectToAction("Announcements");
        }

        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            var a = await _context.Announcements.FindAsync(id);
            if (a != null)
            {
                _context.Announcements.Remove(a);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Announcements");
        }

        // ---------- REVIEW MODERATION ----------
        public async Task<IActionResult> Reviews()
        {
            var reviews = await _context.Reviews
                .Include(r => r.TouristUser)
                .Include(r => r.Destination)
                .Include(r => r.Listing)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(reviews);
        }

        public async Task<IActionResult> ToggleReviewApproval(int id)
        {
            var r = await _context.Reviews.FindAsync(id);
            if (r != null)
            {
                r.IsApproved = !r.IsApproved;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Reviews");
        }

        // ---------- CHECKPOINTS ----------
        public async Task<IActionResult> Checkpoints(int destinationId)
        {
            var destination = await _context.Destinations.FindAsync(destinationId);
            if (destination == null) return NotFound();

            ViewBag.Destination = destination;
            var list = await _context.Checkpoints
                .Where(c => c.DestinationId == destinationId)
                .OrderBy(c => c.SequenceOrder)
                .ToListAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> CreateCheckpoint(int destinationId)
        {
            var destination = await _context.Destinations.FindAsync(destinationId);
            if (destination == null) return NotFound();
            ViewBag.Destination = destination;
            return View(new Checkpoint { DestinationId = destinationId });
        }

        [HttpPost]
        public async Task<IActionResult> CreateCheckpoint(Checkpoint model)
        {
            ModelState.Remove("Destination");
            if (!ModelState.IsValid)
            {
                ViewBag.Destination = await _context.Destinations.FindAsync(model.DestinationId);
                return View(model);
            }

            _context.Checkpoints.Add(model);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Checkpoint added.";
            return RedirectToAction("Checkpoints", new { destinationId = model.DestinationId });
        }

        public async Task<IActionResult> DeleteCheckpoint(int id)
        {
            var c = await _context.Checkpoints.FindAsync(id);
            if (c == null) return NotFound();
            int destId = c.DestinationId;
            _context.Checkpoints.Remove(c);
            await _context.SaveChangesAsync();
            return RedirectToAction("Checkpoints", new { destinationId = destId });
        }

        // ---------- FACILITIES ----------
        public async Task<IActionResult> Facilities()
        {
            var list = await _context.Facilities.ToListAsync();
            return View(list);
        }

        [HttpGet]
        public IActionResult CreateFacility() => View();

        [HttpPost]
        public async Task<IActionResult> CreateFacility(Facility model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Facilities.Add(model);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Facility added.";
            return RedirectToAction("Facilities");
        }

        [HttpGet]
        public async Task<IActionResult> EditFacility(int id)
        {
            var f = await _context.Facilities.FindAsync(id);
            if (f == null) return NotFound();
            return View(f);
        }

        [HttpPost]
        public async Task<IActionResult> EditFacility(Facility model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Facilities.Update(model);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Facility updated.";
            return RedirectToAction("Facilities");
        }

        public async Task<IActionResult> DeleteFacility(int id)
        {
            var f = await _context.Facilities.FindAsync(id);
            if (f != null)
            {
                _context.Facilities.Remove(f);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Facilities");
        }

        // ---------- EMERGENCY CONTACTS ----------
        public async Task<IActionResult> EmergencyContacts()
        {
            var list = await _context.EmergencyContacts.Include(c => c.Destination).ToListAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> CreateEmergencyContact()
        {
            ViewBag.Destinations = await _context.Destinations.OrderBy(d => d.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateEmergencyContact(EmergencyContact model)
        {
            ModelState.Remove("Destination");
            if (!ModelState.IsValid)
            {
                ViewBag.Destinations = await _context.Destinations.OrderBy(d => d.Name).ToListAsync();
                return View(model);
            }
            _context.EmergencyContacts.Add(model);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Emergency contact added.";
            return RedirectToAction("EmergencyContacts");
        }

        public async Task<IActionResult> DeleteEmergencyContact(int id)
        {
            var c = await _context.EmergencyContacts.FindAsync(id);
            if (c != null)
            {
                _context.EmergencyContacts.Remove(c);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("EmergencyContacts");
        }

        // ---------- TOUR PACKAGES ----------
        public async Task<IActionResult> TourPackages()
        {
            var list = await _context.TourPackages.Include(p => p.Destination).OrderByDescending(p => p.CreatedAt).ToListAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> CreateTourPackage()
        {
            ViewBag.Destinations = await _context.Destinations.OrderBy(d => d.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateTourPackage(TourPackage model, IFormFile? imageFile)
        {
            ModelState.Remove("Destination");
            if (!ModelState.IsValid)
            {
                ViewBag.Destinations = await _context.Destinations.OrderBy(d => d.Name).ToListAsync();
                return View(model);
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                model.ImageUrl = await SaveUploadedImage(imageFile);
            }

            _context.TourPackages.Add(model);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Tour package created.";
            return RedirectToAction("TourPackages");
        }

        [HttpGet]
        public async Task<IActionResult> EditTourPackage(int id)
        {
            var p = await _context.TourPackages.FindAsync(id);
            if (p == null) return NotFound();
            ViewBag.Destinations = await _context.Destinations.OrderBy(d => d.Name).ToListAsync();
            return View(p);
        }

        [HttpPost]
        public async Task<IActionResult> EditTourPackage(TourPackage model, IFormFile? imageFile)
        {
            ModelState.Remove("Destination");
            if (!ModelState.IsValid)
            {
                ViewBag.Destinations = await _context.Destinations.OrderBy(d => d.Name).ToListAsync();
                return View(model);
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                model.ImageUrl = await SaveUploadedImage(imageFile);
            }

            _context.TourPackages.Update(model);

            var activeBookingsOnEdit = await _context.Bookings
                .Where(b => b.TourPackageId == model.Id && b.Status != "Cancelled" && b.Status != "Completed")
                .ToListAsync();

            foreach (var booking in activeBookingsOnEdit)
            {
                Vromonsathi.Helpers.NotificationHelper.AddNotification(
                    _context, booking.TouristUserId,
                    "Package updated",
                    $"'{model.Title}' was updated by the admin. Please review the latest details.",
                    "/Tourist/MyBookings");
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Tour package updated.";
            return RedirectToAction("TourPackages");
        }

        public async Task<IActionResult> DeleteTourPackage(int id)
        {
            var p = await _context.TourPackages.FindAsync(id);
            if (p == null) return RedirectToAction("TourPackages");

            var activeBookings = await _context.Bookings
                .Where(b => b.TourPackageId == id && b.Status != "Cancelled" && b.Status != "Completed")
                .ToListAsync();

            bool hasAnyBookingHistory = await _context.Bookings.AnyAsync(b => b.TourPackageId == id);

            if (activeBookings.Any())
            {
                foreach (var booking in activeBookings)
                {
                    var note = $"This package ('{p.Title}') was removed by the admin. Your payment will be refunded to your original payment method within 3-5 business days.";
                    booking.Status = "Cancelled";
                    booking.CancellationNote = note;

                    Vromonsathi.Helpers.NotificationHelper.AddNotification(
                        _context, booking.TouristUserId,
                        "Booking cancelled — refund pending",
                        note,
                        "/Tourist/MyBookings");
                }

                p.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Package removed. {activeBookings.Count} affected booking(s) were cancelled and marked for refund; the package was deactivated instead of deleted since it has booking history.";
                return RedirectToAction("TourPackages");
            }

            if (hasAnyBookingHistory)
            {
                p.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["Message"] = "This package has past booking history, so it can't be permanently deleted. It has been deactivated.";
                return RedirectToAction("TourPackages");
            }

            _context.TourPackages.Remove(p);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Tour package deleted.";
            return RedirectToAction("TourPackages");
        }

        // ---------- PACKAGE BOOKINGS ----------
        public async Task<IActionResult> PackageBookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.TouristUser)
                .Include(b => b.TourPackage)
                .Where(b => b.TourPackageId != null)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        public async Task<IActionResult> UpdatePackageBookingStatus(int id, string status)
        {
            var booking = await _context.Bookings
                .Include(b => b.TourPackage)
                .FirstOrDefaultAsync(b => b.Id == id && b.TourPackageId != null);

            if (booking != null)
            {
                booking.Status = status;

                Vromonsathi.Helpers.NotificationHelper.AddNotification(
                    _context, booking.TouristUserId,
                    $"Package booking {status.ToLower()}",
                    $"Your booking for '{booking.TourPackage!.Title}' is now {status}.",
                    "/Tourist/MyBookings");

                await _context.SaveChangesAsync();
            }
            return RedirectToAction("PackageBookings");
        }
    }
}