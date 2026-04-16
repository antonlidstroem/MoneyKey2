using MoneyKey.Core.DTOs.Vab;

namespace MoneyKey.Blazor.Services.Api;

public class VabApiService : ApiServiceBase
{
    public VabApiService(HttpClient http) : base(http) { }

    public Task<List<VabDto>?> GetAllAsync(int budgetId)             => GetAsync<List<VabDto>>($"api/budgets/{budgetId}/vab");
    public Task<VabDto?>       CreateAsync(int budgetId, CreateVabDto dto) => PostAsync<VabDto>($"api/budgets/{budgetId}/vab", dto);

    public async Task DeleteAsync(int budgetId, int id) =>
        await DeleteAsync($"api/budgets/{budgetId}/vab/{id}");
}
