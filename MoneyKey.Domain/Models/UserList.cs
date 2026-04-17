using MoneyKey.Domain.Enums;

namespace MoneyKey.Domain.Models;

/// <summary>
/// Unified model for both checklists and notes.
/// ContentType = CheckList: uses Items child collection.
/// ContentType = Note: uses Content field (rich text / plain text).
/// Does NOT affect budget calculations regardless of type.
/// </summary>
public class UserList
{
    public int             Id              { get; set; }
    /// <summary>Null when Scope = Personal.</summary>
    public int?            BudgetId        { get; set; }
    public string          Name            { get; set; } = string.Empty;
    /// <summary>CheckList (default) or Note.</summary>
    public ListType        ListType        { get; set; } = ListType.CheckList;
    public string?         Description     { get; set; }
    /// <summary>Rich text / markdown content. Used when ListType = Note.</summary>
    public string?         Content         { get; set; }
    /// <summary>Comma-separated freetext tags, e.g. "arbete,privat,q1".</summary>
    public string?         Tags            { get; set; }
    public EntryScope      Scope           { get; set; } = EntryScope.BudgetSpecific;
    public EntryVisibility Visibility      { get; set; } = EntryVisibility.BudgetShared;
    public DateTime        CreatedAt       { get; set; } = DateTime.UtcNow;
    public DateTime        UpdatedAt       { get; set; } = DateTime.UtcNow;
    public string?         CreatedByUserId { get; set; }
    public bool            IsArchived      { get; set; }

    public Budget?        Budget { get; set; }
    public List<ListItem> Items  { get; set; } = new();
}
