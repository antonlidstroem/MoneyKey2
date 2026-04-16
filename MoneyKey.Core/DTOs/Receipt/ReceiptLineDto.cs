namespace MoneyKey.Core.DTOs.Receipt;

public class ReceiptLineDto
{
    public int      Id                  { get; set; }
    public int      BatchId             { get; set; }
    public int      SequenceNumber      { get; set; }
    public string   ReferenceCode       { get; set; } = string.Empty;
    public DateTime Date                { get; set; }
    public decimal  Amount              { get; set; }
    public string   Currency            { get; set; } = "SEK";
    public string?  Vendor              { get; set; }
    public string?  Description         { get; set; }
    public int?     LinkedTransactionId { get; set; }
    public string?  DigitalReceiptUrl   { get; set; }
}
