using MoneyKey.Domain.Enums;

namespace MoneyKey.Core.DTOs.Lists;

public class UserListDto
{
    public int             Id              { get; set; }
    public int             BudgetId        { get; set; }
    public string          Name            { get; set; } = string.Empty;
    public ListType        ListType        { get; set; }
    public string?         Description     { get; set; }
    public DateTime        CreatedAt       { get; set; }
    public DateTime        UpdatedAt       { get; set; }
    public string?         CreatedByEmail  { get; set; }
    public bool            IsArchived      { get; set; }
    public List<ListItemDto> Items         { get; set; } = new();
    public int             TotalItems      { get; set; }
    public int             CheckedItems    { get; set; }
}
