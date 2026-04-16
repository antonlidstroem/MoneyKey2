using MoneyKey.Core.DTOs.Milersattning;

namespace MoneyKey.Blazor.Services.Api;

public class MilersattningApiService : ApiServiceBase
{
    public MilersattningApiService(HttpClient http) : base(http) { }

    public Task<List<MilersattningDto>?> GetAllAsync(int budgetId)    => GetAsync<List<MilersattningDto>>($"api/budgets/{budgetId}/milersattning");
    public Task<MilersattningDto?>       CreateAsync(int budgetId, CreateMilersattningDto dto) => PostAsync<MilersattningDto>($"api/budgets/{budgetId}/milersattning", dto);

    public async Task DeleteAsync(int budgetId, int id) =>
        await DeleteAsync($"api/budgets/{budgetId}/milersattning/{id}");

    public async Task<decimal> GetRateAsync(int budgetId)
    {
        var r = await GetAsync<RateWrapper>($"api/budgets/{budgetId}/milersattning/rate");
        return r?.Rate ?? 0.25m;
    }

    private class RateWrapper { public decimal Rate { get; set; } }
}
