namespace MoneyKey.Domain.Models;

public class Project
{
    public int       Id           { get; set; }
    public int       BudgetId     { get; set; }
    public string    Name         { get; set; } = string.Empty;
    public string?   Description  { get; set; }
    public decimal   BudgetAmount { get; set; }
    public DateTime  StartDate    { get; set; }
    public DateTime? EndDate      { get; set; }
    public bool      IsActive     { get; set; } = true;
    public DateTime  CreatedAt    { get; set; } = DateTime.UtcNow;

    public Budget   Budget        { get; set; } = null!;
    public ICollection<Transaction>  Transactions  { get; set; } = new List<Transaction>();
    public ICollection<ReceiptBatch> ReceiptBatches { get; set; } = new List<ReceiptBatch>();
}
