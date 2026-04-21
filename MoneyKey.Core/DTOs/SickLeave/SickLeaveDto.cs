using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.SickLeave;
public class SickLeaveDto {
    public int           Id                   { get; set; }
    public DateTime      StartDate            { get; set; }
    public DateTime      EndDate              { get; set; }
    public SickLeaveType SickLeaveType        { get; set; }
    public int           TotalDays            { get; set; }
    public int           KarensDays           { get; set; }
    public int           EmployerDays         { get; set; }
    public int           FkDays               { get; set; }
    public decimal       AnnualSgi            { get; set; }
    public decimal       GrossMonthlySalary   { get; set; }
    public decimal       EmployerSickPay      { get; set; }
    public decimal       FkSickPay            { get; set; }
    public decimal       TotalBenefit         { get; set; }
    public decimal       LostIncome           { get; set; }
    public string?       Notes                { get; set; }
    public bool          IsLinked             => LinkedTransactionId.HasValue;
    public int?          LinkedTransactionId  { get; set; }
}
