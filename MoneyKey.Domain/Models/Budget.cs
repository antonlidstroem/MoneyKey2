using MoneyKey.Domain.Enums;

namespace MoneyKey.Domain.Models;

public class Budget
{
    public int     Id           { get; set; }
    public string  Name         { get; set; } = string.Empty;
    public string? Description  { get; set; }
    public string  OwnerId      { get; set; } = string.Empty;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public bool    IsActive     { get; set; } = true;

    public ICollection<BudgetMembership> Memberships      { get; set; } = new List<BudgetMembership>();
    public ICollection<Transaction>      Transactions     { get; set; } = new List<Transaction>();
    public ICollection<Project>          Projects         { get; set; } = new List<Project>();
    public ICollection<Category>         CustomCategories { get; set; } = new List<Category>();
    public ICollection<AppSetting>       Settings         { get; set; } = new List<AppSetting>();
}
