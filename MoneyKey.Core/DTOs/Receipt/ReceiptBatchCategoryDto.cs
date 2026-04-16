namespace MoneyKey.Core.DTOs.Receipt;

public class ReceiptBatchCategoryDto
{
    public int     Id          { get; set; }
    public string  Name        { get; set; } = string.Empty;
    public string? IconName    { get; set; }
    public string? Description { get; set; }
}
