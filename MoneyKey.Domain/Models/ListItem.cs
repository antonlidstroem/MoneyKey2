namespace MoneyKey.Domain.Models;

public class ListItem
{
    public int      Id          { get; set; }
    public int      ListId      { get; set; }
    public string   Text        { get; set; } = string.Empty;
    public bool     IsChecked   { get; set; }
    public int      SortOrder   { get; set; }
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public UserList? List { get; set; }
}
