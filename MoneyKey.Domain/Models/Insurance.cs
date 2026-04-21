using MoneyKey.Domain.Enums;
namespace MoneyKey.Domain.Models;

public class Insurance
{
    public int                Id            { get; set; }
    public int                BudgetId      { get; set; }
    public string             UserId        { get; set; } = string.Empty;
    public InsuranceType      InsuranceType { get; set; }
    public string             Name          { get; set; } = string.Empty;
    public string?            Provider      { get; set; }
    public decimal            PremiumAmount { get; set; }
    public InsurancePayPeriod PayPeriod     { get; set; } = InsurancePayPeriod.Monthly;
    public DateTime           StartDate     { get; set; }
    public DateTime?          RenewalDate   { get; set; }
    public string?            PolicyNumber  { get; set; }
    public bool               IsActive      { get; set; } = true;
    public string?            Notes         { get; set; }
    public DateTime           CreatedAt     { get; set; } = DateTime.UtcNow;

    public Budget Budget { get; set; } = null!;

    /// <summary>Monthly cost normalised for comparison.</summary>
    public decimal MonthlyCost => PayPeriod switch
    {
        InsurancePayPeriod.Monthly    => PremiumAmount,
        InsurancePayPeriod.Quarterly  => PremiumAmount / 3m,
        InsurancePayPeriod.Yearly     => PremiumAmount / 12m,
        _ => PremiumAmount
    };
}
