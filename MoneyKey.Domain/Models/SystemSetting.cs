namespace MoneyKey.Domain.Models;

/// <summary>
/// Global system-level settings not tied to any budget (no FK to Budgets table).
/// Used for features like the SignalR toggle that affect the whole application.
/// </summary>
public class SystemSetting
{
    public int    Id    { get; set; }
    public string Key   { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
