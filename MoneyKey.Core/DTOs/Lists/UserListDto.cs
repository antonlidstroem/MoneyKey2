using MoneyKey.Domain.Enums;

namespace MoneyKey.Core.DTOs.Lists;

public class UserListDto
{
    public int             Id              { get; set; }
    public int?            BudgetId        { get; set; }
    public string          Name            { get; set; } = string.Empty;
    public ListType        ListType        { get; set; }
    public string?         Description     { get; set; }
    public string?         Content         { get; set; }
    public string?         Tags            { get; set; }
    public EntryScope      Scope           { get; set; }
    public EntryVisibility Visibility      { get; set; }
    public DateTime        CreatedAt       { get; set; }
    public DateTime        UpdatedAt       { get; set; }
    public string?         CreatedByEmail  { get; set; }
    public bool            IsArchived      { get; set; }
    public List<ListItemDto> Items         { get; set; } = new();
    public int             TotalItems      { get; set; }
    public int             CheckedItems    { get; set; }
    /// <summary>True if ListType is Note — items collection is not used.</summary>
    public bool IsNote => ListType == ListType.Note;
}
