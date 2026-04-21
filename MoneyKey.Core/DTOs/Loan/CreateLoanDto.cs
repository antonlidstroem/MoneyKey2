using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Loan;
public class CreateLoanDto {
    public LoanType LoanType       { get; set; }
    public string   Name           { get; set; } = "";
    public string?  LenderName     { get; set; }
    public decimal  OriginalAmount { get; set; }
    public decimal  CurrentBalance { get; set; }
    public decimal  InterestRate   { get; set; }
    public decimal  MonthlyPayment { get; set; }
    public DateTime? PayoffDate    { get; set; }
    public DateTime StartDate      { get; set; } = DateTime.Today;
    public string?  Notes          { get; set; }
}
