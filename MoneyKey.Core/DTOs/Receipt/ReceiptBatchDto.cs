using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Receipt;

public class ReceiptBatchDto
{
    public int      Id               { get; set; }
    public int      BudgetId         { get; set; }
    public int?     ProjectId        { get; set; }
    public string?  ProjectName      { get; set; }
    public string   Label            { get; set; } = string.Empty;
    public int      BatchCategoryId  { get; set; }
    public string   BatchCategoryName { get; set; } = string.Empty;
    public string?  BatchCategoryIcon { get; set; }
    public ReceiptBatchStatus Status { get; set; }
    public string   StatusLabel      { get; set; } = string.Empty;
    public string   CreatedByUserId  { get; set; } = string.Empty;
    public string?  CreatedByEmail   { get; set; }
    public DateTime? SubmittedAt     { get; set; }
    public DateTime? ApprovedAt      { get; set; }
    public DateTime? RejectedAt      { get; set; }
    public string?   RejectionReason { get; set; }
    public DateTime? ReimbursedAt    { get; set; }
    public DateTime  CreatedAt       { get; set; }
    public decimal   TotalAmount     { get; set; }
    public int       LineCount       { get; set; }
    public List<ReceiptLineDto> Lines { get; set; } = new();
}
