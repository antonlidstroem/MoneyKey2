namespace MoneyKey.Core.DTOs.Import;

public class ImportPreviewDto
{
    public List<ImportRowDto> Rows  { get; set; } = new();
    public int TotalRows            { get; set; }
    public int DuplicateCount       { get; set; }
}
