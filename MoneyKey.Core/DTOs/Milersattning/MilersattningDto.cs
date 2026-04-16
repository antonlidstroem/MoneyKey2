namespace MoneyKey.Core.DTOs.Milersattning;

public class MilersattningDto
{
    public int      Id                  { get; set; }
    public int      BudgetId            { get; set; }
    public string   UserId              { get; set; } = string.Empty;
    public string?  UserEmail           { get; set; }
    public DateTime TripDate            { get; set; }
    public string   FromLocation        { get; set; } = string.Empty;
    public string   ToLocation          { get; set; } = string.Empty;
    public decimal  DistanceKm          { get; set; }
    public decimal  RatePerKm           { get; set; }
    public string?  Purpose             { get; set; }
    public decimal  ReimbursementAmount { get; set; }
    public int?     LinkedTransactionId { get; set; }
}
