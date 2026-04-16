namespace MoneyKey.Core.DTOs.Summary;

public class SummaryDto
{
    public decimal FilteredIncome   { get; set; }
    public decimal FilteredExpenses { get; set; }
    public decimal FilteredTotal    => FilteredIncome + FilteredExpenses;
    public decimal MonthlyIncome    { get; set; }
    public decimal MonthlyExpenses  { get; set; }
    public decimal MonthlyTotal     => MonthlyIncome + MonthlyExpenses;
}
