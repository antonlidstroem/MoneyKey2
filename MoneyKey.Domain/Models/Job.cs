using MoneyKey.Domain.Enums;

namespace MoneyKey.Domain.Models;

/// <summary>
/// Describes an income source and its pay type.
/// Prevents double-counting: only one TransactionMode=AutoCreate job per income source.
/// </summary>
public class Job
{
    public int                 Id              { get; set; }
    public int                 BudgetId        { get; set; }
    public string              UserId          { get; set; } = string.Empty;
    public string              Name            { get; set; } = string.Empty;
    /// <summary>Employer or client name.</summary>
    public string?             EmployerName    { get; set; }
    public JobPayType          PayType         { get; set; }
    public JobTransactionMode  TransactionMode { get; set; } = JobTransactionMode.AutoCreate;
    /// <summary>For Monthly/Yearly: fixed gross salary.</summary>
    public decimal?            GrossAmount     { get; set; }
    /// <summary>For Hourly: rate per hour (kr/tim).</summary>
    public decimal?            HourlyRate      { get; set; }
    /// <summary>Optional project link for project-based billing.</summary>
    public int?                ProjectId       { get; set; }
    public bool                IsActive        { get; set; } = true;
    public string?             Notes           { get; set; }
    public DateTime            CreatedAt       { get; set; } = DateTime.UtcNow;

    public Budget              Budget          { get; set; } = null!;
    public Project?            Project         { get; set; }
    public ICollection<TimeEntry> TimeEntries  { get; set; } = new List<TimeEntry>();
}
