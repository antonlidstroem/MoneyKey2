namespace MoneyKey.Core.DTOs.TimeEntry;

public class TimeEntryDto
{
    public int       Id                  { get; set; }
    public int       BudgetId            { get; set; }
    public string    UserId              { get; set; } = string.Empty;
    public int       JobId               { get; set; }
    public string    JobName             { get; set; } = string.Empty;
    public decimal   JobHourlyRate       { get; set; }
    public DateTime  Date                { get; set; }
    public TimeSpan? StartTime           { get; set; }
    public TimeSpan? EndTime             { get; set; }
    public int       DurationMinutes     { get; set; }
    public decimal   DurationHours       => DurationMinutes / 60m;
    public string?   Description         { get; set; }
    public bool      IsBreak             { get; set; }
    public decimal?  HourlyRateOverride  { get; set; }
    public decimal   EffectiveRate       { get; set; }
    public decimal   GrossEarned         { get; set; }
    public bool      IsPosted            => LinkedTransactionId != null;
    public int?      LinkedTransactionId { get; set; }
    public string?   PayrollPeriodKey    { get; set; }
    public DateTime  CreatedAt           { get; set; }
}
