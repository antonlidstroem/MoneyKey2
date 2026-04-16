using MoneyKey.Domain.Enums;

namespace MoneyKey.DAL.Queries;

public class TransactionQuery
{
    public int      BudgetId             { get; set; }
    public int      Page                 { get; set; } = 1;
    public int      PageSize             { get; set; } = 50;
    public string?  SortBy               { get; set; } = "StartDate";
    public string?  SortDir              { get; set; } = "desc";
    public bool     FilterByStartDate    { get; set; }
    public DateTime? StartDate           { get; set; }
    public bool     FilterByEndDate      { get; set; }
    public DateTime? EndDate             { get; set; }
    public bool     FilterByDescription  { get; set; }
    public string?  Description          { get; set; }
    public bool     FilterByAmount       { get; set; }
    public decimal? Amount               { get; set; }
    public bool     FilterByCategory     { get; set; }
    public int?     CategoryId           { get; set; }
    public bool     FilterByRecurrence   { get; set; }
    public Recurrence? Recurrence        { get; set; }
    public bool     FilterByMonth        { get; set; }
    public BudgetMonth? Month            { get; set; }
    public int?     ProjectId            { get; set; }
    public TransactionType? Type         { get; set; }
    public bool?    IsActive             { get; set; }
}
