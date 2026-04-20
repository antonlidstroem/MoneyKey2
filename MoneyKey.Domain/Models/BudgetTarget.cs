namespace MoneyKey.Domain.Models;

/// <summary>Monthly budget goal per category for variance tracking.</summary>
public class BudgetTarget
{
    public int      Id          { get; set; }
    public int      BudgetId    { get; set; }
    public int      CategoryId  { get; set; }
    public int      Year        { get; set; }
    public int      Month       { get; set; }
    public decimal  TargetAmount { get; set; }
    public string?  Notes       { get; set; }
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;

    public Budget   Budget   { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
