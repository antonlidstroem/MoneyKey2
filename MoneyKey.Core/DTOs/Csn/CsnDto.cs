namespace MoneyKey.Core.DTOs.Csn;

public class CsnDto
{
    public int Id { get; set; }
    public int Year { get; set; }
    public decimal TotalOriginalDebt { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AnnualRepayment { get; set; }
    public decimal MonthlyRepayment { get; set; }
    public decimal AnnualIncomeLimit { get; set; }
    public decimal EstimatedAnnualIncome { get; set; }
    public decimal IncomeMargin { get; set; }
    public decimal YearsRemaining { get; set; }
    public bool IsOverIncomeLimit { get; set; }
    public bool IsCurrentlyStudying { get; set; }
    public decimal? MonthlyStudyGrant { get; set; }
    public decimal? MonthlyStudyLoan { get; set; }
    public string? Notes { get; set; }
}