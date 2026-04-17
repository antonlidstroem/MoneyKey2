namespace MoneyKey.Domain.Enums;

/// <summary>
/// Workflow for milersättning reimbursement:
/// Draft → Submitted (sent to payer) → Approved (payer confirmed) → Paid (money received)
/// </summary>
public enum MilersattningStatus { Draft = 0, Submitted = 1, Approved = 2, Paid = 3 }
