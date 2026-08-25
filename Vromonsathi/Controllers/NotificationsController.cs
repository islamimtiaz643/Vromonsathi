using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vromonsathi.Data;
using Vromonsathi.Filters;

namespace Vromonsathi.Controllers
{
    [CustomAuthorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public NotificationsController(ApplicationDbContext context) => _context = context;

        private int CurrentUserId => HttpContext.Session.GetInt32("UserId")!.Value;

        public async Task<IActionResult> Index()
        {
            var list = await _context.Notifications
                .Where(n => n.UserId == CurrentUserId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        public async Task<IActionResult> MarkRead(int id)
        {
            var n = await _context.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId);
            if (n != null)
            {
                n.IsRead = true;
                await _context.SaveChangesAsync();
                if (!string.IsNullOrEmpty(n.LinkUrl)) return Redirect(n.LinkUrl);
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> MarkAllRead()
        {
            var list = await _context.Notifications.Where(n => n.UserId == CurrentUserId && !n.IsRead).ToListAsync();
            foreach (var n in list) n.IsRead = true;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}