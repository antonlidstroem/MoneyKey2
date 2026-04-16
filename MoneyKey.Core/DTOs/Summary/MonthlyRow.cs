namespace MoneyKey.Core.DTOs.Summary;

public class MonthlyRow
{
    public int     Year     { get; set; }
    public int     Month    { get; set; }
    public decimal Income   { get; set; }
    public decimal Expenses { get; set; }
    public decimal Net      => Income + Expenses;
}
