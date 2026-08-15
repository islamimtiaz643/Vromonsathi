using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vromonsathi.Data;
using Vromonsathi.Models;
using Vromonsathi.ViewModels;

namespace Vromonsathi.Controllers
{
    public class EmergencyController : Controller
    {
        private readonly ApplicationDbContext _db;
        public EmergencyController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(int? destinationId)
        {
            var contacts = await _db.EmergencyContacts
                .Where(c => destinationId == null || c.DestinationId == destinationId || c.DestinationId == null)
                .AsNoTracking()
                .ToListAsync();

            return View(contacts);
        }

        [HttpPost]
        public async Task<IActionResult> Sos([FromBody] SosRequest request)
        {
            var nearest = await _db.EmergencyContacts
                .Where(c => c.DestinationId == request.DestinationId)
                .OrderBy(c => c.Type == EmergencyContactType.Police ? 0 : 1)
                .FirstOrDefaultAsync();

            return Json(new
            {
                acknowledged = true,
                routedTo = nearest?.Name ?? "Tourist Police Helpline",
                phone = nearest?.Phone ?? "999",
                receivedAt = DateTime.UtcNow
            });
        }
    }
}