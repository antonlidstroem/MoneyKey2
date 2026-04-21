using MoneyKey.Domain.Enums;

namespace MoneyKey.Core.DTOs.Subscription;

public class UpdateSubscriptionDto
{
    public SubscriptionTier Tier       { get; set; }
    public DateTime?        PaidUntil  { get; set; }
    public string?          PaymentRef { get; set; }
    public bool             IsAdmin    { get; set; }
    public string?          AdminNotes { get; set; }
}
