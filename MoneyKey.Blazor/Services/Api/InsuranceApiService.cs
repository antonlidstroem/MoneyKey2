using System.Net.Http.Json;
using MoneyKey.Core.DTOs.Insurance;
namespace MoneyKey.Blazor.Services.Api;
public class InsuranceApiService : ApiServiceBase
{
    public InsuranceApiService(HttpClient http) : base(http) { }
    public Task<List<InsuranceDto>?> GetAllAsync(int budgetId, bool includeInactive = false) =>
        GetAsync<List<InsuranceDto>>($"api/budgets/{budgetId}/insurances?includeInactive={includeInactive}");
    public async Task<InsuranceDto?> CreateAsync(int budgetId, CreateInsuranceDto dto) {
        var r = await Http.PostAsJsonAsync($"api/budgets/{budgetId}/insurances", dto);
        r.EnsureSuccessStatusCode(); return await r.Content.ReadFromJsonAsync<InsuranceDto>();
    }
    public async Task<InsuranceDto?> UpdateAsync(int budgetId, int id, CreateInsuranceDto dto) {
        var r = await Http.PutAsJsonAsync($"api/budgets/{budgetId}/insurances/{id}", dto);
        r.EnsureSuccessStatusCode(); return await r.Content.ReadFromJsonAsync<InsuranceDto>();
    }
    public async Task DeleteAsync(int budgetId, int id) {
        var r = await Http.DeleteAsync($"api/budgets/{budgetId}/insurances/{id}");
        r.EnsureSuccessStatusCode();
    }
}
