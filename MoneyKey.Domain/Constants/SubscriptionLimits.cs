using MoneyKey.Domain.Enums;

namespace MoneyKey.Domain.Constants;

/// <summary>
/// Subscription tier limits. Only change here — never stored in DB.
/// IsAdmin (SuperAdmin) flag bypasses all limits.
/// </summary>
public static class SubscriptionLimits
{
    public static int MaxBudgets(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free       => 2,
        SubscriptionTier.Plus       => 5,
        SubscriptionTier.Pro        => 10,
        SubscriptionTier.Enterprise => int.MaxValue,
        _ => 2
    };

    public static int MaxMembersPerBudget(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free       => 1,   // owner + 1 invited = 2 people total
        SubscriptionTier.Plus       => 5,
        SubscriptionTier.Pro        => 20,
        SubscriptionTier.Enterprise => int.MaxValue,
        _ => 1
    };

    public static string TierLabel(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free       => "Gratis",
        SubscriptionTier.Plus       => "Plus",
        SubscriptionTier.Pro        => "Pro",
        SubscriptionTier.Enterprise => "Enterprise",
        _ => "Gratis"
    };
}
