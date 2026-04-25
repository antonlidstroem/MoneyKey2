using System.Net.Http.Json;
using MoneyKey.Core.DTOs.SickLeave;

namespace MoneyKey.Blazor.Services.Api;

public class SickLeaveApiService : ApiServiceBase
{
    public SickLeaveApiService(HttpClient http) : base(http) { }

    public Task<List<SickLeaveDto>?> GetAllAsync(int budgetId) =>
        GetAsync<List<SickLeaveDto>>($"api/budgets/{budgetId}/sickleave");

    public async Task<SickLeaveDto?> CreateAsync(int budgetId, CreateSickLeaveDto dto)
    {
        var r = await Http.PostAsJsonAsync($"api/budgets/{budgetId}/sickleave", dto);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<SickLeaveDto>();
    }

    public async Task<SickLeaveDto?> UpdateAsync(int budgetId, int id, CreateSickLeaveDto dto)
    {
        var r = await Http.PutAsJsonAsync($"api/budgets/{budgetId}/sickleave/{id}", dto);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<SickLeaveDto>();
    }

    public async Task DeleteAsync(int budgetId, int id)
    {
        var r = await Http.DeleteAsync($"api/budgets/{budgetId}/sickleave/{id}");
        r.EnsureSuccessStatusCode();
    }
}