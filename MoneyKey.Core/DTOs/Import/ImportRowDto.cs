namespace MoneyKey.Core.DTOs.Import;

public class ImportRowDto
{
    public int      RowIndex               { get; set; }
    public DateTime Date                   { get; set; }
    public decimal  Amount                 { get; set; }
    public string?  Description            { get; set; }
    public bool     IsDuplicate            { get; set; }
    public bool     Selected               { get; set; } = true;
    public int?     SuggestedCategoryId    { get; set; }
    public string?  SuggestedCategoryName  { get; set; }
    public string?  CategoryName           { get; set; }
}
