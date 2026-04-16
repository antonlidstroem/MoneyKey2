using MoneyKey.Core.DTOs.Summary;

namespace MoneyKey.Blazor.Services.Api;

public class ReportsApiService : ApiServiceBase
{
    public ReportsApiService(HttpClient http) : base(http) { }

    public Task<MonthlySummary?> GetMonthlySummaryAsync(int budgetId, int year) =>
        GetAsync<MonthlySummary>($"api/budgets/{budgetId}/reports/monthly-summary?year={year}");

    public Task<List<CategoryBreakdownItem>?> GetCategoryBreakdownAsync(int budgetId, DateTime? from, DateTime? to)
    {
        var url = $"api/budgets/{budgetId}/reports/category-breakdown";
        var sep = "?";
        if (from.HasValue) { url += $"{sep}from={from.Value:yyyy-MM-dd}"; sep = "&"; }
        if (to.HasValue)   { url += $"{sep}to={to.Value:yyyy-MM-dd}"; }
        return GetAsync<List<CategoryBreakdownItem>>(url);
    }
}
