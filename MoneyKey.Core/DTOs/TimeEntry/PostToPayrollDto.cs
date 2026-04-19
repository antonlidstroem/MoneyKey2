namespace MoneyKey.Core.DTOs.TimeEntry;

/// <summary>
/// Payload for the post-to-payroll action.
/// The server creates ONE Transaction (Lön/Income) and stamps all matching entries.
/// </summary>
public class PostToPayrollDto
{
    public int          JobId           { get; set; }
    public string       PayrollPeriodKey { get; set; } = string.Empty; // e.g. "2025-04"
    public List<int>    EntryIds        { get; set; } = new();
    public decimal      GrossAmount     { get; set; }
    public decimal      NetAmount       { get; set; }
    public string?      Description     { get; set; }
}
