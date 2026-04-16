using MoneyKey.Core.DTOs.Kontering;
using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Journal;

public class JournalEntryDto
{
    public JournalEntryType EntryType  { get; set; }
    public string  TypeLabel           { get; set; } = string.Empty;
    public string  TypeCode            { get; set; } = string.Empty;
    public DateTime Date               { get; set; }
    public DateTime? EndDate           { get; set; }
    public decimal Amount              { get; set; }
    public string? Description         { get; set; }
    public string? CategoryName        { get; set; }
    public string? ProjectName         { get; set; }
    public string? Status              { get; set; }
    public string? ReferenceCode       { get; set; }
    public int     SourceId            { get; set; }
    public bool    HasDetail           { get; set; }
    public string? MetaLine            { get; set; }
    public string? CreatedByEmail      { get; set; }
    public List<KonteringRowDto> KonteringRows { get; set; } = new();
    public int?    ReceiptLineCount    { get; set; }
}
