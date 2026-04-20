using MoneyKey.Domain.Enums;
namespace MoneyKey.Domain.Models;

/// <summary>A loan or credit tracked against a budget.</summary>
public class Loan
{
    public int       Id              { get; set; }
    public int       BudgetId        { get; set; }
    public string    UserId          { get; set; } = string.Empty;
    public LoanType  LoanType        { get; set; }
    public string    Name            { get; set; } = string.Empty;
    public string?   LenderName      { get; set; }
    public decimal   OriginalAmount  { get; set; }
    public decimal   CurrentBalance  { get; set; }
    public decimal   InterestRate    { get; set; }  // annual % e.g. 3.5
    public decimal   MonthlyPayment  { get; set; }
    /// <summary>Estimated payoff date based on current balance and payment.</summary>
    public DateTime? PayoffDate      { get; set; }
    public DateTime  StartDate       { get; set; }
    public bool      IsActive        { get; set; } = true;
    public string?   Notes           { get; set; }
    public DateTime  CreatedAt       { get; set; } = DateTime.UtcNow;

    public Budget Budget { get; set; } = null!;

    // Computed (not stored)
    public decimal EffectiveMonthlyRate => InterestRate / 100m / 12m;
    public decimal TotalInterestEstimate =>
    CurrentBalance > 0 && MonthlyPayment > 0
        ? MonthlyPayment * (decimal)EstimatedMonthsLeft - CurrentBalance
        : 0;

    private double EstimatedMonthsLeft =>
        MonthlyPayment > 0 && InterestRate > 0
            ? -Math.Log(1 - (double)(CurrentBalance * EffectiveMonthlyRate / MonthlyPayment))
              / Math.Log(1 + (double)EffectiveMonthlyRate)
            : 0;
}
