namespace MoneyKey.Core.DTOs.Tax;
public class TaxCalculationRequestDto {
    public decimal GrossIncome        { get; set; }
    public decimal MunicipalTaxRate   { get; set; } = 32.0m; // default 32% kommunalskatt
    public int     Year               { get; set; } = DateTime.Today.Year;
    public bool    IsFreelancer       { get; set; }
}
public class TaxCalculationResultDto {
    public decimal GrossIncome          { get; set; }
    public decimal BasicDeduction       { get; set; }  // Grundavdrag
    public decimal JobTaxCredit         { get; set; }  // Jobbskatteavdrag
    public decimal TaxableIncome        { get; set; }
    public decimal MunicipalTax         { get; set; }
    public decimal StateTax             { get; set; }  // Statlig skatt 20%
    public decimal TotalTax             { get; set; }
    public decimal NetIncome            { get; set; }
    public decimal EffectiveTaxRate     { get; set; }
    public decimal SocialFees           { get; set; }  // Egenavgifter if freelancer
    public string  TaxYear              { get; set; } = "";
}
