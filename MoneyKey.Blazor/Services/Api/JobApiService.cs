using System.Net.Http.Json;
using MoneyKey.Core.DTOs.Job;

namespace MoneyKey.Blazor.Services.Api;

public class JobApiService : ApiServiceBase
{
    public JobApiService(HttpClient http) : base(http) { }

    public Task<List<JobDto>?> GetAllAsync(int budgetId, bool includeInactive = false) =>
        GetAsync<List<JobDto>>($"api/budgets/{budgetId}/jobs?includeInactive={includeInactive}");

    public Task<JobDto?> GetByIdAsync(int budgetId, int id) =>
        GetAsync<JobDto>($"api/budgets/{budgetId}/jobs/{id}");

    public async Task<JobDto?> CreateAsync(int budgetId, CreateJobDto dto)
    {
        var r = await Http.PostAsJsonAsync($"api/budgets/{budgetId}/jobs", dto);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<JobDto>();
    }

    public async Task<JobDto?> UpdateAsync(int budgetId, int id, CreateJobDto dto)
    {
        var r = await Http.PutAsJsonAsync($"api/budgets/{budgetId}/jobs/{id}", dto);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<JobDto>();
    }

    public async Task DeleteAsync(int budgetId, int id)
    {
        var r = await Http.DeleteAsync($"api/budgets/{budgetId}/jobs/{id}");
        r.EnsureSuccessStatusCode();
    }
}
