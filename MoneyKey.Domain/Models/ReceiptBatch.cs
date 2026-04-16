using MoneyKey.Domain.Enums;

namespace MoneyKey.Domain.Models;

public class ReceiptBatch
{
    public int       Id               { get; set; }
    public int       BudgetId         { get; set; }
    public int?      ProjectId        { get; set; }
    public string    Label            { get; set; } = string.Empty;
    public int       BatchCategoryId  { get; set; }
    public ReceiptBatchStatus Status  { get; set; } = ReceiptBatchStatus.Draft;
    public string    CreatedByUserId  { get; set; } = string.Empty;
    public string?   CreatedByEmail   { get; set; }
    public DateTime? SubmittedAt      { get; set; }
    public DateTime? ApprovedAt       { get; set; }
    public string?   ApprovedByUserId { get; set; }
    public DateTime? RejectedAt       { get; set; }
    public string?   RejectedByUserId { get; set; }
    public string?   RejectionReason  { get; set; }
    public DateTime? ReimbursedAt     { get; set; }
    public DateTime  CreatedAt        { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt        { get; set; }

    public Budget               Budget   { get; set; } = null!;
    public Project?             Project  { get; set; }
    public ReceiptBatchCategory Category { get; set; } = null!;
    public ICollection<ReceiptLine> Lines { get; set; } = new List<ReceiptLine>();
}
