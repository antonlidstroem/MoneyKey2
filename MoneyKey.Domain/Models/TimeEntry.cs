namespace MoneyKey.Domain.Models;

public class TimeEntry
{
    public int       Id                { get; set; }
    public int       BudgetId          { get; set; }
    public string    UserId            { get; set; } = string.Empty;
    public int       JobId             { get; set; }
    public DateTime  Date              { get; set; }
    public TimeSpan? StartTime         { get; set; }
    public TimeSpan? EndTime           { get; set; }
    /// <summary>
    /// Actual billable minutes. Set explicitly (manual entry) or
    /// computed from StartTime/EndTime and stored on save.
    /// </summary>
    public int       DurationMinutes   { get; set; }
    public string?   Description       { get; set; }
    public bool      IsBreak           { get; set; }
    /// <summary>
    /// Rate override for this entry. If null, falls back to Job.HourlyRate.
    /// </summary>
    public decimal?  HourlyRateOverride { get; set; }
    /// <summary>
    /// Set when this entry has been included in a payroll post.
    /// Prevents double-posting.
    /// </summary>
    public int?      LinkedTransactionId { get; set; }
    /// <summary>e.g. "2025-04" — groups entries into one payout batch.</summary>
    public string?   PayrollPeriodKey  { get; set; }
    public DateTime  CreatedAt         { get; set; } = DateTime.UtcNow;

    // ── Navigation ────────────────────────────────────────────────────────────
    public Budget       Budget           { get; set; } = null!;
    public Job          Job              { get; set; } = null!;
    public Transaction? LinkedTransaction { get; set; }

    // ── Computed (not stored) ─────────────────────────────────────────────────
    /// <summary>Effective hourly rate: override ?? Job.HourlyRate ?? 0</summary>
    public decimal EffectiveRate(decimal jobRate) => HourlyRateOverride ?? jobRate;
    public decimal GrossEarned(decimal jobRate)   => IsBreak ? 0 : DurationMinutes / 60m * EffectiveRate(jobRate);
}
