namespace MoneyKey.Domain.Enums;

/// <summary>
/// BudgetSpecific = visible only in the budget it was created in.
/// Personal       = personal note, not tied to any budget, only the creator sees it.
/// </summary>
public enum EntryScope { BudgetSpecific = 0, Personal = 1 }
