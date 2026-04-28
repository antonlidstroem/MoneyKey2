using MoneyKey.Domain.Enums;

namespace MoneyKey.Domain.Models;

public class UserList
{
    public int Id { get; set; }
    public int? BudgetId { get; set; }
    public Budget? Budget { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ListType ListType { get; set; } = ListType.CheckList;
    public string? Description { get; set; }
    public string? Content { get; set; }
    public string? Tags { get; set; }
    public EntryScope Scope { get; set; } = EntryScope.BudgetSpecific;
    public EntryVisibility Visibility { get; set; } = EntryVisibility.Private;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; }
    public List<ListItem> Items { get; set; } = new();
    public string? ListConfig { get; set; }

    // Computed helpers
    public bool IsNote => ListType == ListType.Note;
}