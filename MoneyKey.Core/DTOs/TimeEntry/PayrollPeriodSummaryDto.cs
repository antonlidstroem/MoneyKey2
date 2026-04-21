namespace MoneyKey.Core.DTOs.TimeEntry;

/// <summary>Groups unposted entries by month for the payroll review screen.</summary>
public class PayrollPeriodSummaryDto
{
    public string         PeriodKey     { get; set; } = string.Empty; // "2025-04"
    public string         PeriodLabel   { get; set; } = string.Empty; // "April 2025"
    public int            JobId         { get; set; }
    public string         JobName       { get; set; } = string.Empty;
    public decimal        HourlyRate    { get; set; }
    public decimal        TotalHours    { get; set; }
    public decimal        GrossAmount   { get; set; }
    public List<TimeEntryDto> Entries   { get; set; } = new();
}
