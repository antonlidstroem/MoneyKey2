using MoneyKey.Domain.Enums;

namespace MoneyKey.Domain.Models;

public class AuditLog
{
    public int        Id         { get; set; }
    public int        BudgetId   { get; set; }
    public string?    UserId     { get; set; }
    public string?    UserEmail  { get; set; }
    public string     EntityName { get; set; } = string.Empty;
    public string     EntityId   { get; set; } = string.Empty;
    public AuditAction Action    { get; set; }
    public string?    OldValues  { get; set; }
    public string?    NewValues  { get; set; }
    public DateTime   Timestamp  { get; set; } = DateTime.UtcNow;
}
