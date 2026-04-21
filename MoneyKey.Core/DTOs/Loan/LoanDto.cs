using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Loan;
public class LoanDto {
    public int      Id             { get; set; }
    public LoanType LoanType       { get; set; }
    public string   TypeLabel      { get; set; } = "";
    public string   Name           { get; set; } = "";
    public string?  LenderName     { get; set; }
    public decimal  OriginalAmount { get; set; }
    public decimal  CurrentBalance { get; set; }
    public decimal  InterestRate   { get; set; }
    public decimal  MonthlyPayment { get; set; }
    public DateTime? PayoffDate    { get; set; }
    public DateTime StartDate      { get; set; }
    public bool     IsActive       { get; set; }
    public string?  Notes          { get; set; }
    public decimal  PaidAmount     => OriginalAmount - CurrentBalance;
    public double   ProgressPct    => OriginalAmount > 0 ? (double)(PaidAmount / OriginalAmount * 100) : 0;
    public decimal  TotalInterestEstimate { get; set; }
    public int      EstimatedMonthsLeft   { get; set; }
}
