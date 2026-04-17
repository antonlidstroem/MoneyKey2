using MoneyKey.Domain.Enums;

namespace MoneyKey.Core.DTOs.Milersattning;

public class MilersattningDto
{
    public int                 Id                  { get; set; }
    public int                 BudgetId            { get; set; }
    public string              UserId              { get; set; } = string.Empty;
    public string?             UserEmail           { get; set; }
    public DateTime            TripDate            { get; set; }
    public string              FromLocation        { get; set; } = string.Empty;
    public string              ToLocation          { get; set; } = string.Empty;
    public decimal             DistanceKm          { get; set; }
    public bool                IsRoundTrip         { get; set; }
    public decimal             EffectiveDistanceKm { get; set; }
    public decimal             RatePerKm           { get; set; }
    public string?             Purpose             { get; set; }
    public string?             PayerName           { get; set; }
    public MilersattningStatus Status              { get; set; }
    public string              StatusLabel         { get; set; } = string.Empty;
    public decimal             ReimbursementAmount { get; set; }
    public int?                LinkedTransactionId { get; set; }
    /// <summary>True if RatePerKm exceeds the Skatteverket standard (0.25 kr/km).</summary>
    public bool                RateAboveStandard   => RatePerKm > 0.25m;
}
