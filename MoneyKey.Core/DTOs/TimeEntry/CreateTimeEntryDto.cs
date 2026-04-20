namespace MoneyKey.Core.DTOs.TimeEntry;

public class CreateTimeEntryDto
{
    public int       JobId              { get; set; }
    public DateTime  Date               { get; set; } = DateTime.Today;
    public TimeSpan? StartTime          { get; set; }
    public TimeSpan? EndTime            { get; set; }
    public int       DurationMinutes    { get; set; }
    public string?   Description        { get; set; }
    public bool      IsBreak            { get; set; }
    public decimal?  HourlyRateOverride { get; set; }
}
