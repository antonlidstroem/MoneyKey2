using System.Net.Http.Json;
using MoneyKey.Core.DTOs.Project;

namespace MoneyKey.Blazor.Services.Api;

public class ProjectService : ApiServiceBase
{
    public ProjectService(HttpClient http) : base(http) { }

    public Task<List<ProjectDto>?> GetAllAsync(int budgetId) =>
        GetAsync<List<ProjectDto>>($"api/budgets/{budgetId}/projects");

    public Task<ProjectDto?> GetByIdAsync(int budgetId, int id) =>
        GetAsync<ProjectDto>($"api/budgets/{budgetId}/projects/{id}");

    public Task<ProjectDto?> CreateAsync(int budgetId, CreateProjectDto dto) =>
        PostAsync<ProjectDto>($"api/budgets/{budgetId}/projects", dto);

    public async Task<ProjectDto?> UpdateAsync(int budgetId, UpdateProjectDto dto)
    {
        var r = await Http.PutAsJsonAsync($"api/budgets/{budgetId}/projects/{dto.Id}", dto);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ProjectDto>();
    }

    public async Task DeleteAsync(int budgetId, int id) =>
        await DeleteAsync($"api/budgets/{budgetId}/projects/{id}");
}
