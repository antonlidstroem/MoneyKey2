using MoneyKey.Domain.Enums;

namespace MoneyKey.Domain.Models;

/// <summary>
/// Pending, accepted or declined invitation to a budget.
/// Invitations are found by DisplayName lookup — email is never exposed.
/// </summary>
public class BudgetInvitation
{
    public int                    Id              { get; set; }
    public int                    BudgetId        { get; set; }
    public string                 InvitedByUserId { get; set; } = string.Empty;
    public string                 InvitedUserId   { get; set; } = string.Empty;
    public BudgetMemberRole       Role            { get; set; } = BudgetMemberRole.Viewer;
    public BudgetInvitationStatus Status          { get; set; } = BudgetInvitationStatus.Pending;
    public DateTime               CreatedAt       { get; set; } = DateTime.UtcNow;
    public DateTime               ExpiresAt       { get; set; } = DateTime.UtcNow.AddDays(7);

    public Budget Budget { get; set; } = null!;
}
