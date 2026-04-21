namespace MoneyKey.Core.DTOs.BudgetTarget;
public class UpsertBudgetTargetDto {
    public int     CategoryId   { get; set; }
    public int     Year         { get; set; }
    public int     Month        { get; set; }
    public decimal TargetAmount { get; set; }
    public string? Notes        { get; set; }
}
