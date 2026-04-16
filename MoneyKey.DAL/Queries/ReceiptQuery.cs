using MoneyKey.Domain.Enums;

namespace MoneyKey.DAL.Queries;

public class ReceiptQuery
{
    public int     BudgetId        { get; set; }
    public int     Page            { get; set; } = 1;
    public int     PageSize        { get; set; } = 50;
    public string? SortBy          { get; set; } = "CreatedAt";
    public string? SortDir         { get; set; } = "desc";
    public string? LabelSearch     { get; set; }
    public int?    BatchCategoryId { get; set; }
    public int?    ProjectId       { get; set; }
    public string? CreatedByUserId { get; set; }
    public List<ReceiptBatchStatus>? Statuses { get; set; }
    public DateTime? FromDate      { get; set; }
    public DateTime? ToDate        { get; set; }
}
