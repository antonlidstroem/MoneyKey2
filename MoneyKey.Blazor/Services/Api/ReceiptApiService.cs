using System.Net.Http.Json;
using MoneyKey.Core.DTOs.Common;
using MoneyKey.Core.DTOs.Receipt;
using MoneyKey.Domain.Enums;

namespace MoneyKey.Blazor.Services.Api;

public class ReceiptApiService : ApiServiceBase
{
    public ReceiptApiService(HttpClient http) : base(http) { }

    public Task<PagedResult<ReceiptBatchDto>?> GetAllAsync(
        int budgetId, int page = 1, int pageSize = 25, ReceiptBatchStatus? status = null)
    {
        var url = $"api/budgets/{budgetId}/receipts?page={page}&pageSize={pageSize}";
        if (status.HasValue) url += $"&statuses={(int)status.Value}";
        return GetAsync<PagedResult<ReceiptBatchDto>>(url);
    }

    public Task<ReceiptBatchDto?> GetByIdAsync(int budgetId, int batchId) =>
        GetAsync<ReceiptBatchDto>($"api/budgets/{budgetId}/receipts/{batchId}");

    public Task<ReceiptBatchDto?> CreateAsync(int budgetId, CreateReceiptBatchDto dto) =>
        PostAsync<ReceiptBatchDto>($"api/budgets/{budgetId}/receipts", dto);

    public async Task UpdateAsync(int budgetId, int batchId, UpdateReceiptBatchDto dto)
    {
        var r = await Http.PutAsJsonAsync($"api/budgets/{budgetId}/receipts/{batchId}", dto);
        r.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(int budgetId, int batchId) =>
        await DeleteAsync($"api/budgets/{budgetId}/receipts/{batchId}");

    public Task<ReceiptLineDto?> AddLineAsync(int budgetId, int batchId, CreateReceiptLineDto dto) =>
        PostAsync<ReceiptLineDto>($"api/budgets/{budgetId}/receipts/{batchId}/lines", dto);

    public async Task DeleteLineAsync(int budgetId, int batchId, int lineId) =>
        await DeleteAsync($"api/budgets/{budgetId}/receipts/{batchId}/lines/{lineId}");

    public async Task<ReceiptBatchDto?> UpdateStatusAsync(
        int budgetId, int batchId, ReceiptBatchStatus newStatus, string? rejectionReason = null)
    {
        var r = await Http.PatchAsJsonAsync(
            $"api/budgets/{budgetId}/receipts/{batchId}/status",
            new UpdateReceiptStatusDto(newStatus, rejectionReason));
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ReceiptBatchDto>();
    }

    public Task<List<ReceiptBatchCategoryDto>?> GetCategoriesAsync(int budgetId) =>
        GetAsync<List<ReceiptBatchCategoryDto>>($"api/budgets/{budgetId}/receipt-categories");

    public string GetPdfUrl(int budgetId, int batchId) =>
        $"api/budgets/{budgetId}/receipts/{batchId}/export/pdf";

    /// <summary>
    /// Returns all receipt batches for a budget (summary only — for dropdowns).
    /// </summary>
    public Task<List<ReceiptBatchDto>?> GetAllBatchesAsync(int budgetId) =>
        GetAsync<List<ReceiptBatchDto>>($"api/budgets/{budgetId}/receipts");
}
