using System.Net.Http.Json;
using MoneyKey.Core.DTOs.TimeEntry;

namespace MoneyKey.Blazor.Services.Api;

public class TimeEntryApiService : ApiServiceBase
{
    public TimeEntryApiService(HttpClient http) : base(http) { }

    public Task<List<TimeEntryDto>?> GetAllAsync(int budgetId, int? jobId = null, DateTime? from = null, DateTime? to = null)
    {
        var url = $"api/budgets/{budgetId}/timeentries?";
        if (jobId.HasValue) url += $"jobId={jobId}&";
        if (from.HasValue)  url += $"from={from:yyyy-MM-dd}&";
        if (to.HasValue)    url += $"to={to:yyyy-MM-dd}&";
        return GetAsync<List<TimeEntryDto>>(url.TrimEnd('?', '&'));
    }

    public async Task<TimeEntryDto?> CreateAsync(int budgetId, CreateTimeEntryDto dto)
    {
        var r = await Http.PostAsJsonAsync($"api/budgets/{budgetId}/timeentries", dto);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<TimeEntryDto>();
    }

    public async Task<TimeEntryDto?> UpdateAsync(int budgetId, int id, CreateTimeEntryDto dto)
    {
        var r = await Http.PutAsJsonAsync($"api/budgets/{budgetId}/timeentries/{id}", dto);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<TimeEntryDto>();
    }

    public async Task DeleteAsync(int budgetId, int id)
    {
        var r = await Http.DeleteAsync($"api/budgets/{budgetId}/timeentries/{id}");
        r.EnsureSuccessStatusCode();
    }

    public Task<PayrollPeriodSummaryDto?> GetPeriodSummaryAsync(int budgetId, int jobId, string period) =>
        GetAsync<PayrollPeriodSummaryDto>($"api/budgets/{budgetId}/timeentries/period-summary?jobId={jobId}&period={period}");

    public async Task<PostToPayrollResultDto?> PostToPayrollAsync(int budgetId, PostToPayrollDto dto)
    {
        var r = await Http.PostAsJsonAsync($"api/budgets/{budgetId}/timeentries/post-to-payroll", dto);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<PostToPayrollResultDto>();
    }
}

public class PostToPayrollResultDto
{
    public int     TransactionId { get; set; }
    public decimal GrossAmount   { get; set; }
    public decimal NetAmount     { get; set; }
    public string  Description   { get; set; } = "";
}
