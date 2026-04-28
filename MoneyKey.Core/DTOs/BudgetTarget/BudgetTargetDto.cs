namespace MoneyKey.Core.DTOs.BudgetTarget;

public class BudgetTargetDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal? ActualAmount { get; set; }

    // Computed properties (in DTO, not in domain):
    public decimal Variance => TargetAmount - (ActualAmount ?? 0);
    public int UsedPct => TargetAmount > 0
                        ? (int)((ActualAmount ?? 0) / TargetAmount * 100)
                        : 0;
    public bool IsOverBudget => (ActualAmount ?? 0) > TargetAmount && TargetAmount > 0;
}