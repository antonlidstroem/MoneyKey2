using MoneyKey.Core.DTOs.Kontering;
using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Transaction;

public class TransactionDto
{
    public int       Id              { get; set; }
    public int       BudgetId        { get; set; }
    public DateTime  StartDate       { get; set; }
    public DateTime? EndDate         { get; set; }
    public decimal   NetAmount       { get; set; }
    public decimal?  GrossAmount     { get; set; }
    public string?   Description     { get; set; }
    public int       CategoryId      { get; set; }
    public string    CategoryName    { get; set; } = string.Empty;
    public TransactionType Type      { get; set; }
    public Recurrence Recurrence     { get; set; }
    public bool      IsActive        { get; set; }
    public BudgetMonth? Month        { get; set; }
    public decimal?  Rate            { get; set; }
    public int?      ProjectId       { get; set; }
    public string?   ProjectName     { get; set; }
    public bool      HasKontering    { get; set; }
    public string?   CreatedByUserId { get; set; }
    public DateTime  CreatedAt       { get; set; }
    public List<KonteringRowDto> KonteringRows { get; set; } = new();
}
