namespace MoneyKey.Core.DTOs.Csn;

public class CreateCsnDto
{
    public int Year { get; set; }
    public decimal TotalOriginalDebt { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AnnualRepayment { get; set; }
    public decimal AnnualIncomeLimit { get; set; }
    public decimal EstimatedAnnualIncome { get; set; }
    public bool IsCurrentlyStudying { get; set; }
    public decimal? MonthlyStudyGrant { get; set; }
    public decimal? MonthlyStudyLoan { get; set; }
    public string? Notes { get; set; }
}