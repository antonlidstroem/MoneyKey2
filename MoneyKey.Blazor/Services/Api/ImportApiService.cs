using System.Net.Http.Json;
using MoneyKey.Core.DTOs.Import;

namespace MoneyKey.Blazor.Services.Api;

public class ImportApiService : ApiServiceBase
{
    public ImportApiService(HttpClient http) : base(http) { }

    public async Task<ImportSessionDto?> PreviewAsync(int budgetId, Stream stream, string fileName, string bankProfile)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", fileName);
        var r = await Http.PostAsync($"api/budgets/{budgetId}/import/preview?bankProfile={bankProfile}", content);
        r.EnsureSuccessStatusCode();
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
