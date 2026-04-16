using System.Net.Http.Json;
using MoneyKey.Core.DTOs.Common;
using MoneyKey.Core.DTOs.Journal;
using MoneyKey.Core.DTOs.Summary;

namespace MoneyKey.Blazor.Services.Api;

public class JournalApiService : ApiServiceBase
{
    public JournalApiService(HttpClient http) : base(http) { }

    public async Task<(PagedResult<JournalEntryDto> Result, SummaryDto Summary)?> GetPagedAsync(
        int budgetId, JournalQuery query)
    {
        var url      = BuildUrl(budgetId, query);
        var response = await Http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        var raw = await response.Content.ReadFromJsonAsync<JournalPageResponse>();
        return raw == null ? null : (raw.Result, raw.Summary);
    }

    public string GetPdfUrl(int budgetId, JournalQuery q)   => $"api/budgets/{budgetId}/transactions/export/pdf?{FilterParams(q)}";
    public string GetExcelUrl(int budgetId, JournalQuery q) => $"api/budgets/{budgetId}/transactions/export/excel?{FilterParams(q)}";

    private static string BuildUrl(int budgetId, JournalQuery q)
    {
        var parts = new List<string>
        {
            $"page={q.Page}", $"pageSize={q.PageSize}",
            $"sortBy={Uri.EscapeDataString(q.SortBy ?? "Date")}",
            $"sortDir={Uri.EscapeDataString(q.SortDir ?? "desc")}"
        };
        foreach (var t in q.IncludeTypes)     parts.Add($"includeTypes={t}");
        foreach (var s in q.ReceiptStatuses)  parts.Add($"receiptStatuses={s}");
        parts.AddRange(FilterParts(q));
        return $"api/budgets/{budgetId}/journal?" + string.Join("&", parts);
    }

    private static string FilterParams(JournalQuery q) => string.Join("&", FilterParts(q));

    private static List<string> FilterParts(JournalQuery q)
    {
        var p = new List<string>();
        if (q.FilterByStartDate && q.StartDate.HasValue)
            p.Add($"filterByStartDate=true&startDate={q.StartDate.Value:yyyy-MM-dd}");
        if (q.FilterByEndDate && q.EndDate.HasValue)
            p.Add($"filterByEndDate=true&endDate={q.EndDate.Value:yyyy-MM-dd}");
        if (q.FilterByDescription && !string.IsNullOrWhiteSpace(q.Description))
            p.Add($"filterByDescription=true&description={Uri.EscapeDataString(q.Description)}");
        if (q.FilterByCategory && q.CategoryId.HasValue)
            p.Add($"filterByCategory=true&categoryId={q.CategoryId.Value}");
        if (q.FilterByProject && q.ProjectId.HasValue)
            p.Add($"filterByProject=true&projectId={q.ProjectId.Value}");
        return p;
    }

    private class JournalPageResponse
    {
        public PagedResult<JournalEntryDto> Result  { get; set; } = new();
        public SummaryDto                   Summary { get; set; } = new();
    }
}
