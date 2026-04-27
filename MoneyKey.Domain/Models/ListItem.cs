namespace MoneyKey.Domain.Models;

public class ListItem
{
    public int Id { get; set; }
    public int ListId { get; set; }
    public UserList List { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public bool IsChecked { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// JSON-serialised type-specific item payload.
    /// E.g. PackingItemData, HabitItemData etc.
    /// Null for legacy items.
    /// </summary>
    public string? ItemData { get; set; }
}