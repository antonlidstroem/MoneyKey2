namespace MoneyKey.Core.DTOs.BudgetTarget;

public class BudgetTargetDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    // Tillägg i BudgetTargetDto.cs:
    public decimal? ActualAmount { get; set; }

    // Beräknade egenskaper (i DTO, inte i domain):
    public decimal Variance => TargetAmount - (ActualAmount ?? 0);
    public int UsedPct => TargetAmount > 0
                                      ? (int)((ActualAmount ?? 0) / TargetAmount * 100)
                                      : 0;
    public bool IsOverBudget => (ActualAmount ?? 0) > TargetAmount && TargetAmount > 0;
}

public class UpsertBudgetTargetDto
{
    public int CategoryId { get; set; }
    public decimal TargetAmount { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}