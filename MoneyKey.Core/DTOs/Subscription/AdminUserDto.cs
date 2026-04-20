using MoneyKey.Domain.Enums;

namespace MoneyKey.Core.DTOs.Subscription;

public class AdminUserDto
{
    public string           UserId      { get; set; } = string.Empty;
    public string           Email       { get; set; } = string.Empty;
    public string           DisplayName { get; set; } = string.Empty;
    public string?          FirstName   { get; set; }
    public string?          LastName    { get; set; }
    public SubscriptionTier Tier        { get; set; }
    public string           TierLabel   { get; set; } = string.Empty;
    public DateTime?        PaidUntil   { get; set; }
    public bool             IsActive    { get; set; }
    public bool             IsAdmin     { get; set; }
    public string?          AdminNotes  { get; set; }
    public int              BudgetCount { get; set; }
    public DateTime         CreatedAt   { get; set; }
}
