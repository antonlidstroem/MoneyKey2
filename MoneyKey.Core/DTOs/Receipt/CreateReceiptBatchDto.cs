namespace MoneyKey.Core.DTOs.Receipt;
public record CreateReceiptBatchDto(string Label, int BatchCategoryId, int? ProjectId);
