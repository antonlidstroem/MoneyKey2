using System.Net.Http.Json;
using MoneyKey.Core.DTOs.Csn;

namespace MoneyKey.Blazor.Services.Api;

public class CsnApiService : ApiServiceBase
{
    public CsnApiService(HttpClient http) : base(http) { }

    public Task<List<CsnDto>?> GetAllAsync(int budgetId) =>
        GetAsync<List<CsnDto>>($"api/budgets/{budgetId}/csn");

    public async Task<CsnDto?> CreateAsync(int budgetId, CreateCsnDto dto)
    {
        var r = await Http.PostAsJsonAsync($"api/budgets/{budgetId}/csn", dto);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<CsnDto>();
    }

    public async Task<CsnDto?> UpdateAsync(int budgetId, int id, CreateCsnDto dto)
    {
        var r = await Http.PutAsJsonAsync($"api/budgets/{budgetId}/csn/{id}", dto);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<CsnDto>();
    }

    public async Task DeleteAsync(int budgetId, int id)
    {
        var r = await Http.DeleteAsync($"api/budgets/{budgetId}/csn/{id}");
        r.EnsureSuccessStatusCode();
    }
}