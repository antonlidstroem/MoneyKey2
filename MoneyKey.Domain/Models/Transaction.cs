using MoneyKey.Domain.Enums;

namespace MoneyKey.Domain.Models;

public class Transaction
{
    public int       Id                   { get; set; }
    public int       BudgetId             { get; set; }
    public DateTime  StartDate            { get; set; }
    public DateTime? EndDate              { get; set; }
    public decimal   NetAmount            { get; set; }
    public decimal?  GrossAmount          { get; set; }
    public string?   Description          { get; set; }
    public int       CategoryId           { get; set; }
    public Recurrence Recurrence          { get; set; }
    public bool      IsActive             { get; set; } = true;
    public BudgetMonth? Month             { get; set; }
    public decimal?  Rate                 { get; set; }
    public TransactionType Type           { get; set; }
    public int?      ProjectId            { get; set; }
    public bool      HasKontering         { get; set; } = false;
    public int?      MilersattningEntryId { get; set; }
    public int?      VabEntryId           { get; set; }
    // ── Receipt tracking ─────────────────────────────────────────────────────
    public ReceiptStatus ReceiptStatus   { get; set; } = ReceiptStatus.NotRequired;
    public string?       WaivedReason    { get; set; }
    public string?   CreatedByUserId      { get; set; }
    public string?   UpdatedByUserId      { get; set; }
    public DateTime  CreatedAt            { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt            { get; set; }

    public Budget    Budget       { get; set; } = null!;
    public Category  Category     { get; set; } = null!;
    public Project?  Project      { get; set; }
    public ICollection<KonteringRow> KonteringRows { get; set; } = new List<KonteringRow>();
}
