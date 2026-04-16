namespace MoneyKey.Core.DTOs.Import;
public record ConfirmImportDto(List<int> SelectedRowIndices, int DefaultCategoryId, string SessionId);
