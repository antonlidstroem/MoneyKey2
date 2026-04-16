namespace MoneyKey.API.Hubs;

public record BudgetEvent(string EventType, int BudgetId, int? EntityId, string? UpdatedByEmail, DateTime Timestamp);
