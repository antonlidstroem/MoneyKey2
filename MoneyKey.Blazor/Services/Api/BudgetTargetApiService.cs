using System.Net.Http.Json;
using MoneyKey.Core.DTOs.BudgetTarget;
namespace MoneyKey.Blazor.Services.Api;
public class BudgetTargetApiService : ApiServiceBase
{
    public BudgetTargetApiService(HttpClient http) : base(http) { }
    public Task<List<BudgetTargetDto>?> GetAllAsync(int budgetId, int year, int month) =>
        GetAsync<List<BudgetTargetDto>>($"api/budgets/{budgetId}/targets?year={year}&month={month}");
    public async Task UpsertAsync(int budgetId, UpsertBudgetTargetDto dto) {
        var r = await Http.PutAsJsonAsync($"api/budgets/{budgetId}/targets", dto);
        r.EnsureSuccessStatusCode();
    }
    public async Task DeleteAsync(int budgetId, int id) {
        var r = await Http.DeleteAsync($"api/budgets/{budgetId}/targets/{id}");
        r.EnsureSuccessStatusCode();
    }
}
