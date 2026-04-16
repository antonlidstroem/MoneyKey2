using MoneyKey.Domain.Enums;

namespace MoneyKey.Domain.Models;

/// <summary>
/// A user-created list (to-do, shopping, etc.) that is visible in the journal
/// but has NO impact on the budget calculations.
/// </summary>
public class UserList
{
    public int       Id              { get; set; }
    public int       BudgetId        { get; set; }
    public string    Name            { get; set; } = string.Empty;
    public ListType  ListType        { get; set; } = ListType.Custom;
    public string?   Description     { get; set; }
    public DateTime  CreatedAt       { get; set; } = DateTime.UtcNow;
    public DateTime  UpdatedAt       { get; set; } = DateTime.UtcNow;
    public string?   CreatedByUserId { get; set; }
    public bool      IsArchived      { get; set; }

    public Budget?          Budget { get; set; }
    public List<ListItem>   Items  { get; set; } = new();
}
