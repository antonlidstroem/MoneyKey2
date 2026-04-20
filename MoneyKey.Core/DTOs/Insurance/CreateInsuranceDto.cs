using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Insurance;
public class CreateInsuranceDto {
    public InsuranceType      InsuranceType { get; set; }
    public string             Name          { get; set; } = "";
    public string?            Provider      { get; set; }
    public decimal            PremiumAmount { get; set; }
    public InsurancePayPeriod PayPeriod     { get; set; }
    public DateTime           StartDate     { get; set; } = DateTime.Today;
    public DateTime?          RenewalDate   { get; set; }
    public string?            PolicyNumber  { get; set; }
    public string?            Notes         { get; set; }
}
