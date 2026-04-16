namespace MoneyKey.Core.DTOs.Project;

public class ProjectDto
{
    public int       Id             { get; set; }
    public int       BudgetId       { get; set; }
    public string    Name           { get; set; } = string.Empty;
    public string?   Description    { get; set; }
    public decimal   BudgetAmount   { get; set; }
    public DateTime  StartDate      { get; set; }
    public DateTime? EndDate        { get; set; }
    public bool      IsActive       { get; set; }
    public decimal   SpentAmount    { get; set; }
    public decimal   RemainingAmount => BudgetAmount + SpentAmount;
    public double    ProgressPercent => BudgetAmount == 0 ? 0 : Math.Min(100, (double)(-SpentAmount / BudgetAmount * 100));
}
