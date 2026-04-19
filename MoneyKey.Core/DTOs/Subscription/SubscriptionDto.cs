using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Constants;

namespace MoneyKey.Core.DTOs.Subscription;

public class SubscriptionDto
{
    public string           UserId        { get; set; } = string.Empty;
    public SubscriptionTier Tier          { get; set; }
    public string           TierLabel     { get; set; } = string.Empty;
    public DateTime?        PaidUntil     { get; set; }
    public bool             IsActive      { get; set; }
    public bool             IsAdmin       { get; set; }
    public int              MaxBudgets    { get; set; }
    public int              MaxMembers    { get; set; }
    public int              UsedBudgets   { get; set; }
    // Display name is here so the whole profile is one call
    public string           DisplayName   { get; set; } = string.Empty;
}
