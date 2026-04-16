using MoneyKey.Core.DTOs.Kontering;
using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Transaction;

public class CreateTransactionDto
{
    public DateTime  StartDate     { get; set; } = DateTime.Today;
    public DateTime? EndDate       { get; set; }
    public decimal   NetAmount     { get; set; }
    public decimal?  GrossAmount   { get; set; }
    public string?   Description   { get; set; }
    public int       CategoryId    { get; set; }
    public TransactionType Type    { get; set; }
    public Recurrence Recurrence   { get; set; }
    public bool      IsActive      { get; set; } = true;
    public BudgetMonth? Month      { get; set; }
    public decimal?  Rate          { get; set; }
    public int?      ProjectId     { get; set; }
    public List<KonteringRowDto> KonteringRows { get; set; } = new();
}
