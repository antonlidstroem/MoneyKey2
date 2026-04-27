namespace MoneyKey.Core.DTOs.Lists;

public class CreateListItemDto
{
    public string Text { get; set; } = string.Empty;
    public string? ItemData { get; set; }  // JSON payload — ny
    public int SortOrder { get; set; }

    public CreateListItemDto() { }
    public CreateListItemDto(string text) => Text = text;
}