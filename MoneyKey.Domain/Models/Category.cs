using MoneyKey.Domain.Enums;

namespace MoneyKey.Domain.Models;

public class Category
{
    public int     Id               { get; set; }
    public string  Name             { get; set; } = string.Empty;
    public TransactionType Type     { get; set; }
    public bool    ToggleGrossNet   { get; set; } = false;
    public int?    DefaultRate      { get; set; }
    public AdjustmentType? AdjustmentType { get; set; }
    public string? Description      { get; set; }
    public bool    HasEndDate       { get; set; } = false;
    public bool    IsSystemCategory { get; set; } = true;
    public int?    BudgetId         { get; set; }
    public string? IconName         { get; set; }
    /// <summary>When true, transactions in this category default to ReceiptStatus.Required.</summary>
    public bool    IsReceiptRequired { get; set; }
}
