namespace MoneyKey.Core.DTOs.Vab;

public class VabDto
{
    public int      Id                  { get; set; }
    public int      BudgetId            { get; set; }
    public string   UserId              { get; set; } = string.Empty;
    public string?  UserEmail           { get; set; }
    public string?  ChildName           { get; set; }
    public DateTime StartDate           { get; set; }
    public DateTime EndDate             { get; set; }
    public decimal  DailyBenefit        { get; set; }
    public decimal  Rate                { get; set; }
    public int      TotalDays           { get; set; }
    public decimal  TotalAmount         { get; set; }
    public int?     LinkedTransactionId { get; set; }
}
