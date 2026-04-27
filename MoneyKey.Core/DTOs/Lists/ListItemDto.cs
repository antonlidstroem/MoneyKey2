namespace MoneyKey.Core.DTOs.Lists;

public class ListItemDto
{
    public int     Id          { get; set; }
    public int     ListId      { get; set; }
    public string  Text        { get; set; } = string.Empty;
    public bool    IsChecked   { get; set; }
    public int     SortOrder   { get; set; }
    public DateTime CreatedAt  { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Tillägg till befintlig ListItemDto:
    public string? ItemData { get; set; }    // JSON — typ-specifikt payload
}
