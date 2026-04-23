using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.SickLeave;
public class CreateSickLeaveDto {
    public DateTime      StartDate          { get; set; } = DateTime.Today;
    public DateTime      EndDate            { get; set; } = DateTime.Today;
    public SickLeaveType SickLeaveType      { get; set; }
    public decimal       AnnualSgi          { get; set; }
    public decimal       GrossMonthlySalary { get; set; }
    public string?       Notes              { get; set; }
}
