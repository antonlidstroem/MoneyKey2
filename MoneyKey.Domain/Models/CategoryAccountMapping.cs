namespace MoneyKey.Domain.Models;

/// <summary>Maps a MoneyKey category to a BAS account number for SIE4 export.</summary>
public class CategoryAccountMapping
{
    public int     Id         { get; set; }
    public int     BudgetId   { get; set; }
    public int     CategoryId { get; set; }
    public string  BasAccount { get; set; } = string.Empty;  // e.g. "4010"
    public string  AccountName{ get; set; } = string.Empty;  // e.g. "Inköp varor"
    public Budget   Budget   { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
