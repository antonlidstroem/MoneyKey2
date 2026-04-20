using MoneyKey.Domain.Enums;

namespace MoneyKey.Core.DTOs.Job;

public class JobDto
{
    public int                Id              { get; set; }
    public int                BudgetId        { get; set; }
    public string             UserId          { get; set; } = string.Empty;
    public string             Name            { get; set; } = string.Empty;
    public string?            EmployerName    { get; set; }
    public JobPayType         PayType         { get; set; }
    public string             PayTypeLabel    { get; set; } = string.Empty;
    public JobTransactionMode TransactionMode { get; set; }
    public decimal?           GrossAmount     { get; set; }
    public decimal?           HourlyRate      { get; set; }
    public int?               ProjectId       { get; set; }
    public string?            ProjectName     { get; set; }
    public bool               IsActive        { get; set; }
    public string?            Notes           { get; set; }
    public DateTime           CreatedAt       { get; set; }
    /// <summary>Total unposted hours for this job.</summary>
    public decimal            UnpostedHours   { get; set; }
    public decimal            UnpostedGross   { get; set; }
}
