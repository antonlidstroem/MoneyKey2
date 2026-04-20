using MoneyKey.Domain.Enums;

namespace MoneyKey.Core.DTOs.Invitation;

/// <summary>Invite by DisplayName — never by email.</summary>
public class SendInvitationDto
{
    public string           DisplayName { get; set; } = string.Empty;
    public BudgetMemberRole Role        { get; set; } = BudgetMemberRole.Viewer;
}
