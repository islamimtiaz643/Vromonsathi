using Vromonsathi.ViewModels;

namespace Vromonsathi.Services
{
    public interface IBudgetCalculatorService
    {
        Task<BudgetEstimateResult> EstimateAsync(BudgetEstimateRequest request);
        Task<List<GroupCostingRow>> BuildGroupCostingTableAsync(int destinationId, int days, List<string> facilities, int[] groupSizes);
    }
}