using MoneyKey.Domain.Enums;

namespace MoneyKey.Domain.Models;

public class MilersattningEntry
{
    public int                  Id                  { get; set; }
    public int                  BudgetId            { get; set; }
    public string               UserId              { get; set; } = string.Empty;
    public DateTime             TripDate            { get; set; }
    public string               FromLocation        { get; set; } = string.Empty;
    public string               ToLocation          { get; set; } = string.Empty;
    /// <summary>One-way distance in km. Use EffectiveDistanceKm for calculations.</summary>
    public decimal              DistanceKm          { get; set; }
    /// <summary>True if the same route is driven both ways (tur-retur).</summary>
    public bool                 IsRoundTrip         { get; set; }
    /// <summary>Effective distance used in calculation: DistanceKm * (IsRoundTrip ? 2 : 1).</summary>
    public decimal              EffectiveDistanceKm => DistanceKm * (IsRoundTrip ? 2m : 1m);
    /// <summary>
    /// Rate per km. Skatteverket 2024 standard = 0.25 kr/km (25 öre/km).
    /// The tax-free limit is 2.50 kr/km for private car. Values above 0.25 are employer-set.
    /// </summary>
    public decimal              RatePerKm           { get; set; } = 0.25m;
    public string?              Purpose             { get; set; }
    /// <summary>Who owes the reimbursement (employer name, client, etc.).</summary>
    public string?              PayerName           { get; set; }
    public MilersattningStatus  Status              { get; set; } = MilersattningStatus.Draft;
    public DateTime?            SubmittedAt         { get; set; }
    public DateTime?            ApprovedAt          { get; set; }
    public DateTime?            PaidAt              { get; set; }
    public decimal              ReimbursementAmount => EffectiveDistanceKm * RatePerKm;
    public int?                 LinkedTransactionId { get; set; }
    public DateTime             CreatedAt           { get; set; } = DateTime.UtcNow;

    public Budget       Budget            { get; set; } = null!;
    public Transaction? LinkedTransaction { get; set; }
}
