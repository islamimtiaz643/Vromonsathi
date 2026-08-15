using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vromonsathi.Data;
using Vromonsathi.Helpers;
using Vromonsathi.Models;
using Vromonsathi.ViewModels;

namespace Vromonsathi.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            bool emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            PasswordHelper.CreatePasswordHash(model.Password, out string hash, out string salt);

            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = model.Role,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (model.Role == "Vendor")
            {
                var vendorProfile = new VendorProfile
                {
                    UserId = user.Id,
                    BusinessName = model.BusinessName ?? "Unnamed Business",
                    BusinessType = model.BusinessType ?? "Hotel",
                    IsApproved = false
                };
                _context.VendorProfiles.Add(vendorProfile);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Registration successful. Your vendor account needs admin approval before you can list services.";
            }
            else
            {
                TempData["Message"] = "Registration successful. Please log in.";
            }

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !PasswordHelper.VerifyPassword(model.Password, user.PasswordHash, user.PasswordSalt))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Your account has been deactivated. Contact admin.");
                return View(model);
            }

            // Manual session-based login
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Role", user.Role);
            HttpContext.Session.SetString("FullName", user.FullName);

            if (user.Role == "Admin")
                return RedirectToAction("Dashboard", "Admin");
            if (user.Role == "Vendor")
                return RedirectToAction("Dashboard", "Vendor");

            return RedirectToAction("Dashboard", "Tourist");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}