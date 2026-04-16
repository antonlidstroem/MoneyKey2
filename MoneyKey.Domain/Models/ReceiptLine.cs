namespace MoneyKey.Domain.Models;

public class ReceiptLine
{
    public int       Id                  { get; set; }
    public int       BatchId             { get; set; }
    public int       SequenceNumber      { get; set; }
    public string    ReferenceCode       { get; set; } = string.Empty;
    public DateTime  Date                { get; set; }
    public decimal   Amount              { get; set; }
    public string    Currency            { get; set; } = "SEK";
    public string?   Vendor              { get; set; }
    public string?   Description         { get; set; }
    public int?      LinkedTransactionId { get; set; }
    public string?   AttachmentPath      { get; set; }
    public string?   AttachmentMimeType  { get; set; }
    public string?   DigitalReceiptUrl   { get; set; }
    public DateTime  CreatedAt           { get; set; } = DateTime.UtcNow;

    public ReceiptBatch Batch           { get; set; } = null!;
    public Transaction? LinkedTransaction { get; set; }
}
