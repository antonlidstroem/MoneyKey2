namespace MoneyKey.Domain.Models;

public class KonteringRow
{
    public int       Id            { get; set; }
    public int       TransactionId { get; set; }
    public string    KontoNr       { get; set; } = string.Empty;
    public string?   CostCenter    { get; set; }
    public decimal   Amount        { get; set; }
    public decimal?  Percentage    { get; set; }
    public string?   Description   { get; set; }
    public Transaction Transaction  { get; set; } = null!;
}
