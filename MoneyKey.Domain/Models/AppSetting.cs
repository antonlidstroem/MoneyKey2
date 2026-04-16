namespace MoneyKey.Domain.Models;

public class AppSetting
{
    public int    Id       { get; set; }
    public int    BudgetId { get; set; }
    public string Key      { get; set; } = string.Empty;
    public string Value    { get; set; } = string.Empty;
    public Budget Budget   { get; set; } = null!;
}
