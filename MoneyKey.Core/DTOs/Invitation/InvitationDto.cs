using MoneyKey.Domain.Enums;

namespace MoneyKey.Core.DTOs.Invitation;

public class InvitationDto
{
    public int                    Id              { get; set; }
    public int                    BudgetId        { get; set; }
    public string                 BudgetName      { get; set; } = string.Empty;
    public string                 InvitedByName   { get; set; } = string.Empty;
    public BudgetMemberRole       Role            { get; set; }
    public BudgetInvitationStatus Status          { get; set; }
    public DateTime               CreatedAt       { get; set; }
    public DateTime               ExpiresAt       { get; set; }
}
