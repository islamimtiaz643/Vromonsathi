using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vromonsathi.Data;

namespace Vromonsathi.Controllers
{
    public class FacilitiesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public FacilitiesController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            return View(await _db.Facilities.AsNoTracking().ToListAsync());
        }
    }
}