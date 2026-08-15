using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vromonsathi.Data;
using Vromonsathi.Models;

namespace Vromonsathi.Controllers
{
    public class RoutesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public RoutesController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Checkpoints(int destinationId)
        {
            var checkpoints = await _db.Checkpoints
                .Where(c => c.DestinationId == destinationId)
                .OrderBy(c => c.SequenceOrder)
                .AsNoTracking()
                .ToListAsync();

            var summary = new
            {
                ArmyCamps = checkpoints.Count(c => c.Type == CheckpointType.Army),
                BgbCamps = checkpoints.Count(c => c.Type == CheckpointType.BGB),
                PoliceChecks = checkpoints.Count(c => c.Type == CheckpointType.Police),
                Route = checkpoints
            };

            return Json(summary);
        }
    }
}