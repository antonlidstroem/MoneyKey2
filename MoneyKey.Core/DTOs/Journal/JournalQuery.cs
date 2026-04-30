using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Journal;

public class JournalQuery
{
    public int     BudgetId            { get; set; }
    public int     Page                { get; set; } = 1;
    public int     PageSize            { get; set; } = 50;
    public string? SortBy              { get; set; } = "Date";
    public string? SortDir             { get; set; } = "desc";
    public List<JournalEntryType> IncludeTypes { get; set; } = new();
    public bool     FilterByStartDate  { get; set; }
    public DateTime? StartDate         { get; set; }
    public bool     FilterByEndDate    { get; set; }
    public DateTime? EndDate           { get; set; }
    public bool     FilterByDescription { get; set; }
    public string?  Description        { get; set; }
    public bool     FilterByCategory   { get; set; }
    public int?     CategoryId         { get; set; }
    public bool     FilterByProject    { get; set; }
    public int?     ProjectId          { get; set; }
    public bool     FilterByAmount     { get; set; }
    public decimal? AmountMin          { get; set; }
    public decimal? AmountMax          { get; set; }
    public bool     FilterByCreatedBy  { get; set; }
    public string?  CreatedByUserId    { get; set; }
    public string? QuickSearch { get; set; }
    public List<ReceiptBatchStatus> ReceiptStatuses { get; set; } = new();

}
