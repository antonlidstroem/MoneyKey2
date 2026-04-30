using System.Net.Http.Json;
using MoneyKey.Core.DTOs.Transaction;

namespace MoneyKey.Blazor.Services.Api;

public class TransactionService : ApiServiceBase
{
    public TransactionService(HttpClient http) : base(http) { }

    public Task<TransactionDto?> CreateAsync(int budgetId, CreateTransactionDto dto) =>
        PostAsync<TransactionDto>($"api/budgets/{budgetId}/transactions", dto);

    public async Task<TransactionDto?> UpdateAsync(int budgetId, UpdateTransactionDto dto)
    {
        var r = await Http.PutAsJsonAsync($"api/budgets/{budgetId}/transactions/{dto.Id}", dto);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<TransactionDto>();
    }

    public async Task DeleteAsync(int budgetId, int id) =>
        await DeleteAsync($"api/budgets/{budgetId}/transactions/{id}");

    public async Task BatchDeleteAsync(int budgetId, List<int> ids)
    {
        var r = await Http.PostAsJsonAsync($"api/budgets/{budgetId}/transactions/batch-delete", new BatchDeleteDto(ids));
        r.EnsureSuccessStatusCode();
    }

    public Task<TransactionDto?> GetByIdAsync(int budgetId, int id) =>
    GetAsync<TransactionDto>($"api/budgets/{budgetId}/transactions/{id}");
}
