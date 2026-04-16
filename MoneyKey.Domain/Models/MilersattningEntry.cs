namespace MoneyKey.Domain.Models;

public class MilersattningEntry
{
    public int       Id                  { get; set; }
    public int       BudgetId            { get; set; }
    public string    UserId              { get; set; } = string.Empty;
    public DateTime  TripDate            { get; set; }
    public string    FromLocation        { get; set; } = string.Empty;
    public string    ToLocation          { get; set; } = string.Empty;
    public decimal   DistanceKm          { get; set; }
    public decimal   RatePerKm           { get; set; } = 0.25m;
    public string?   Purpose             { get; set; }
    public decimal   ReimbursementAmount => DistanceKm * RatePerKm;
    public int?      LinkedTransactionId { get; set; }
    public DateTime  CreatedAt           { get; set; } = DateTime.UtcNow;

    public Budget       Budget          { get; set; } = null!;
    public Transaction? LinkedTransaction { get; set; }
}
