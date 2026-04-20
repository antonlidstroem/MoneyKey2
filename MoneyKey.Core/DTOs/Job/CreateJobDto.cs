using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Job;

public class CreateJobDto
{
    public string             Name            { get; set; } = string.Empty;
    public string?            EmployerName    { get; set; }
    public JobPayType         PayType         { get; set; }
    public JobTransactionMode TransactionMode { get; set; } = JobTransactionMode.AutoCreate;
    public decimal?           GrossAmount     { get; set; }
    public decimal?           HourlyRate      { get; set; }
    public int?               ProjectId       { get; set; }
    public bool               IsActive        { get; set; } = true;
    public string?            Notes           { get; set; }
}
