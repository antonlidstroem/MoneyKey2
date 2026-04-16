using System.Net.Http.Json;
using MoneyKey.Core.DTOs.Budget;
using MoneyKey.Domain.Models;

namespace MoneyKey.Blazor.Services.Api;

public class BudgetService : ApiServiceBase
{
    public BudgetService(HttpClient http) : base(http) { }

    public Task<List<BudgetDto>?>  GetMyBudgetsAsync()           => GetAsync<List<BudgetDto>>("api/budgets");
    public Task<BudgetDto?>        CreateAsync(CreateBudgetDto dto) => PostAsync<BudgetDto>("api/budgets", dto);
    public Task<List<Category>?>   GetCategoriesAsync(int budgetId) => GetAsync<List<Category>>($"api/budgets/{budgetId}/categories");

    public async Task InviteAsync(int budgetId, InviteMemberDto dto) =>
        await PostAsync<object>($"api/budgets/{budgetId}/invite", dto);

    public async Task UpdateAsync(int budgetId, UpdateBudgetDto dto)
    {
        var r = await Http.PutAsJsonAsync($"api/budgets/{budgetId}", dto);
        r.EnsureSuccessStatusCode();
    }
}
