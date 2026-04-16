using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Audit;

public class AuditLogDto
{
    public int        Id         { get; set; }
    public string?    UserEmail  { get; set; }
    public string     EntityName { get; set; } = string.Empty;
    public string     EntityId   { get; set; } = string.Empty;
    public AuditAction Action    { get; set; }
    public string?    OldValues  { get; set; }
    public string?    NewValues  { get; set; }
    public DateTime   Timestamp  { get; set; }
}
