namespace MoneyKey.Core.DTOs.Receipt;
public record UpdateReceiptLineDto(DateTime Date, decimal Amount, string? Vendor, string? Description);
