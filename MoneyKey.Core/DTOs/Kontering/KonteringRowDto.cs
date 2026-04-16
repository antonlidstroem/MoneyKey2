namespace MoneyKey.Core.DTOs.Kontering;

public class KonteringRowDto
{
    public int      Id          { get; set; }
    public string   KontoNr     { get; set; } = string.Empty;
    public string?  CostCenter  { get; set; }
    public decimal  Amount      { get; set; }
    public decimal? Percentage  { get; set; }
    public string?  Description { get; set; }
}
