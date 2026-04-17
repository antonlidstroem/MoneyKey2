namespace MoneyKey.Domain.Enums;

/// <summary>
/// Private     = only the creator sees it.
/// BudgetShared = all members with Viewer+ role on the linked budget can see it.
/// </summary>
public enum EntryVisibility { Private = 0, BudgetShared = 1 }
