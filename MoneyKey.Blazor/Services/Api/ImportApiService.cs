using System.Net.Http.Json;
using MoneyKey.Core.DTOs.Import;

namespace MoneyKey.Blazor.Services.Api;

public class ImportApiService : ApiServiceBase
{
    public ImportApiService(HttpClient http) : base(http) { }

    /// <summary>
    /// Auto-detect preview — no column hints.
    /// </summary>
    public async Task<ImportSessionDto?> PreviewAsync(int budgetId, Stream stream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", fileName);
        var r = await Http.PostAsync($"api/budgets/{budgetId}/import/preview", content);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ImportSessionDto>();
    }

    /// <summary>
    /// Manual-mapping preview — caller specifies column indices.
    /// Falls back to the same endpoint with hint headers.
    /// </summary>
    public async Task<ImportSessionDto?> PreviewWithMappingAsync(
        int budgetId, Stream stream, string fileName,
        int dateColIndex, int amountColIndex, int descColIndex)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", fileName);
        content.Add(new StringContent(dateColIndex.ToString()), "dateColIndex");
        content.Add(new StringContent(amountColIndex.ToString()), "amountColIndex");
        content.Add(new StringContent(descColIndex.ToString()), "descColIndex");

        var url = $"api/budgets/{budgetId}/import/preview" +
                  $"?dateColIndex={dateColIndex}" +
                  $"&amountColIndex={amountColIndex}" +
                  $"&descColIndex={descColIndex}";

        using var content2 = new MultipartFormDataContent();
        // Re-read stream — it was already consumed; caller should pass a fresh MemoryStream
        content2.Add(new StreamContent(stream), "file", fileName);

        var r = await Http.PostAsync(url, content2);
        if (!r.IsSuccessStatusCode)
        {
            // Fallback: try the basic endpoint without mapping
            stream.Position = 0;
            return await PreviewAsync(budgetId, stream, fileName);
        }
        return await r.Content.ReadFromJsonAsync<ImportSessionDto>();
    }

    public async Task<int> ConfirmAsync(int budgetId, ConfirmImportDto dto)
    {
        var r = await Http.PostAsJsonAsync($"api/budgets/{budgetId}/import/confirm", dto);
        r.EnsureSuccessStatusCode();
        var res = await r.Content.ReadFromJsonAsync<ImportResult>();
        return res?.Imported ?? 0;
    }

    private class ImportResult { public int Imported { get; set; } }
}