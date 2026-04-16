namespace MoneyKey.Domain.Models;

public class VabEntry
{
    public int       Id                  { get; set; }
    public int       BudgetId            { get; set; }
    public string    UserId              { get; set; } = string.Empty;
    public string?   ChildName           { get; set; }
    public DateTime  StartDate           { get; set; }
    public DateTime  EndDate             { get; set; }
    public decimal   DailyBenefit        { get; set; }
    public decimal   Rate                { get; set; } = 0.80m;
    public int       TotalDays           => Math.Max(1, (int)(EndDate - StartDate).TotalDays + 1);
    public decimal   TotalAmount         => TotalDays * DailyBenefit * Rate;
    public int?      LinkedTransactionId { get; set; }
    public DateTime  CreatedAt           { get; set; } = DateTime.UtcNow;

    public Budget       Budget          { get; set; } = null!;
    public Transaction? LinkedTransaction { get; set; }
}
