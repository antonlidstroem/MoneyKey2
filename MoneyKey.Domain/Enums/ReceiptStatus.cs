namespace MoneyKey.Domain.Enums;
/// <summary>Whether this transaction needs a receipt and whether one is attached.</summary>
public enum ReceiptStatus
{
    NotRequired = 0,
    Required    = 1,  // Required but not yet attached
    Attached    = 2,  // Receipt attached via ReceiptBatch
    Waived      = 3   // Explicitly waived with reason
}
