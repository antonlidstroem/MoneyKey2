namespace MoneyKey.Core.DTOs.Sie;
public class SieExportRequestDto {
    public int      BudgetId      { get; set; }
    public int      Year          { get; set; }
    public string   CompanyName   { get; set; } = "";
    public string?  OrgNumber     { get; set; }
    public bool     IncludeVat    { get; set; }
}
