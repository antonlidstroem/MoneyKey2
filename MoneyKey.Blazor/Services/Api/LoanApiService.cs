using System.Net.Http.Json;
using MoneyKey.Core.DTOs.Loan;
namespace MoneyKey.Blazor.Services.Api;
public class LoanApiService : ApiServiceBase
{
    public LoanApiService(HttpClient http) : base(http) { }
    public Task<List<LoanDto>?> GetAllAsync(int budgetId, bool includeInactive = false) =>
        GetAsync<List<LoanDto>>($"api/budgets/{budgetId}/loans?includeInactive={includeInactive}");
    public async Task<LoanDto?> CreateAsync(int budgetId, CreateLoanDto dto) {
        var r = await Http.PostAsJsonAsync($"api/budgets/{budgetId}/loans", dto);
        r.EnsureSuccessStatusCode(); return await r.Content.ReadFromJsonAsync<LoanDto>();
    }
    public async Task<LoanDto?> UpdateAsync(int budgetId, int id, CreateLoanDto dto) {
        var r = await Http.PutAsJsonAsync($"api/budgets/{budgetId}/loans/{id}", dto);
        r.EnsureSuccessStatusCode(); return await r.Content.ReadFromJsonAsync<LoanDto>();
    }
    public async Task DeleteAsync(int budgetId, int id) {
        var r = await Http.DeleteAsync($"api/budgets/{budgetId}/loans/{id}");
        r.EnsureSuccessStatusCode();
    }
}
