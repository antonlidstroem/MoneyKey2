using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Insurance;
public class InsuranceDto {
    public int                Id            { get; set; }
    public InsuranceType      InsuranceType { get; set; }
    public string             TypeLabel     { get; set; } = "";
    public string             Name          { get; set; } = "";
    public string?            Provider      { get; set; }
    public decimal            PremiumAmount { get; set; }
    public InsurancePayPeriod PayPeriod     { get; set; }
    public string             PayPeriodLabel{ get; set; } = "";
    public decimal            MonthlyCost   { get; set; }
    public DateTime           StartDate     { get; set; }
    public DateTime?          RenewalDate   { get; set; }
    public bool               RenewalSoon   => RenewalDate.HasValue && RenewalDate.Value <= DateTime.Today.AddDays(30);
    public string?            PolicyNumber  { get; set; }
    public bool               IsActive      { get; set; }
    public string?            Notes         { get; set; }
}
