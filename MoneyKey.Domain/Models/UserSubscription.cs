using MoneyKey.Domain.Enums;

namespace MoneyKey.Domain.Models;

/// <summary>
/// 1:1 with ApplicationUser. Stores subscription tier and admin flag.
/// Created automatically as Free tier on user registration.
/// </summary>
public class UserSubscription
{
    public string            UserId       { get; set; } = string.Empty;
    public SubscriptionTier  Tier         { get; set; } = SubscriptionTier.Free;
    public DateTime?         PaidUntil    { get; set; }
    public string?           PaymentRef   { get; set; }
    public bool              IsAdmin      { get; set; }
    public string?           AdminNotes   { get; set; }
    public DateTime          CreatedAt    { get; set; } = DateTime.UtcNow;
    public DateTime          UpdatedAt    { get; set; } = DateTime.UtcNow;

    public bool IsActive => PaidUntil == null || PaidUntil >= DateTime.UtcNow
                         || Tier == SubscriptionTier.Free;
}
