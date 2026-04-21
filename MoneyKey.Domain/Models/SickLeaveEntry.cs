using MoneyKey.Domain.Enums;
namespace MoneyKey.Domain.Models;

/// <summary>
/// Tracks own sick leave (not VAB).
/// Day 1 = karensdag (no pay). Day 2-14 = employer pays 80% of salary.
/// Day 15+ = FK pays 80% of SGI.
/// </summary>
public class SickLeaveEntry
{
    public int           Id             { get; set; }
    public int           BudgetId       { get; set; }
    public string        UserId         { get; set; } = string.Empty;
    public DateTime      StartDate      { get; set; }
    public DateTime      EndDate        { get; set; }
    public SickLeaveType SickLeaveType  { get; set; }
    public decimal       AnnualSgi      { get; set; }  // SGI for FK calculation
    public decimal       GrossMonthlySalary { get; set; }
    public string?       Diagnosis      { get; set; }  // optional, sensitive
    public string?       Notes          { get; set; }
    public int?          LinkedTransactionId { get; set; }
    public DateTime      CreatedAt      { get; set; } = DateTime.UtcNow;

    public Budget Budget { get; set; } = null!;

    public int TotalDays => (int)(EndDate - StartDate).TotalDays + 1;
    public int KarensDays => Math.Min(1, TotalDays);
    public int EmployerDays => Math.Max(0, Math.Min(TotalDays - 1, 13));  // day 2-14
    public int FkDays => Math.Max(0, TotalDays - 14);                      // day 15+

    /// <summary>Gross daily salary for employer sick pay calculation.</summary>
    public decimal GrossDailyFromSalary => GrossMonthlySalary * 12m / 365m;
    public decimal EmployerSickPay => EmployerDays * GrossDailyFromSalary * 0.8m;
    public decimal FkSickPay => FkDays * (AnnualSgi / 365m) * 0.8m;
    public decimal TotalBenefit => EmployerSickPay + FkSickPay;
    /// <summary>Income lost compared to normal salary.</summary>
    public decimal LostIncome => TotalDays * GrossDailyFromSalary - TotalBenefit;
}
