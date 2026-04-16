namespace MoneyKey.Core.DTOs.Receipt;
public record CreateReceiptLineDto(int BudgetId, DateTime Date, decimal Amount, string? Vendor, string? Description);
