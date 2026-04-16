namespace MoneyKey.Blazor.State;

public class BudgetHubEvent
{
    public string   EventType      { get; set; } = string.Empty;
    public int      BudgetId       { get; set; }
    public int?     EntityId       { get; set; }
    public string?  UpdatedByEmail { get; set; }
    public DateTime Timestamp      { get; set; }
}
