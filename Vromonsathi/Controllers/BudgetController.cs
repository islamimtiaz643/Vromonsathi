using Microsoft.AspNetCore.Mvc;
using Vromonsathi.Services;
using Vromonsathi.ViewModels;

namespace Vromonsathi.Controllers
{
    public class BudgetController : Controller
    {
        private readonly IBudgetCalculatorService _budget;

        public BudgetController(IBudgetCalculatorService budget)
        {
            _budget = budget;
        }

        public IActionResult Index()
        {
            return Redirect("/#estimator");
        }

        [HttpPost]
        public async Task<IActionResult> Estimate([FromBody] BudgetEstimateRequest request)
        {
            if (request == null || request.DestinationId == 0)
                return BadRequest("A destination is required.");

            try
            {
                var result = await _budget.EstimateAsync(request);
                return Json(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GroupCosting(int destinationId, int days = 3)
        {
            var rows = await _budget.BuildGroupCostingTableAsync(
                destinationId, days,
                new List<string> { "Hotel", "Guide" },
                new[] { 2, 4, 8, 12 });

            return Json(rows);
        }
    }
}