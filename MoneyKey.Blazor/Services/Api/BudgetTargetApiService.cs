using System.Net.Http.Json;
using MoneyKey.Core.DTOs.BudgetTarget;

namespace MoneyKey.Blazor.Services.Api;

public class BudgetTargetApiService : ApiServiceBase
{
    public BudgetTargetApiService(HttpClient http) : base(http) { }

    public Task<List<BudgetTargetDto>?> GetForMonthAsync(
        int budgetId, int year, int month, bool includeActuals = true) =>
        GetAsync<List<BudgetTargetDto>>(
            $"api/budgets/{budgetId}/targets?year={year}&month={month}&includeActuals={includeActuals}");

    public async Task<BudgetTargetDto?> UpsertAsync(int budgetId, UpsertBudgetTargetDto dto)
    {
        var r = await Http.PostAsJsonAsync($"api/budgets/{budgetId}/targets", dto);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<BudgetTargetDto>();
    }

    public async Task DeleteAsync(int budgetId, int id)
    {
        var r = await Http.DeleteAsync($"api/budgets/{budgetId}/targets/{id}");
        r.EnsureSuccessStatusCode();
    }
}