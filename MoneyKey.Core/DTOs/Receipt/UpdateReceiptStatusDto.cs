using MoneyKey.Domain.Enums;
namespace MoneyKey.Core.DTOs.Receipt;
public record UpdateReceiptStatusDto(ReceiptBatchStatus NewStatus, string? RejectionReason);
