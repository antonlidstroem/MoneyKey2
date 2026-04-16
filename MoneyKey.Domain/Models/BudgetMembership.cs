using MoneyKey.Domain.Enums;

namespace MoneyKey.Domain.Models;

public class BudgetMembership
{
    public int      Id              { get; set; }
    public int      BudgetId        { get; set; }
    public string   UserId          { get; set; } = string.Empty;
    public BudgetMemberRole Role    { get; set; }
    public string?  InvitedByUserId { get; set; }
    public string?  InviteToken     { get; set; }
    public DateTime InvitedAt       { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt     { get; set; }
    public bool     IsAccepted      => AcceptedAt.HasValue;
    public Budget   Budget          { get; set; } = null!;
}
