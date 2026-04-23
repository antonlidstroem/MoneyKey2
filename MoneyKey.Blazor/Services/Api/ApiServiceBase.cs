using System.Net.Http.Json;
using System.Text.Json;

namespace MoneyKey.Blazor.Services.Api;

public abstract class ApiServiceBase
{
    protected readonly HttpClient Http;
    protected ApiServiceBase(HttpClient http) => Http = http;

    protected Task<T?> GetAsync<T>(string url) => Http.GetFromJsonAsync<T>(url);

    protected async Task<T?> PostAsync<T>(string url, object body)
    {
        var r = await Http.PostAsJsonAsync(url, body);
        if (!r.IsSuccessStatusCode)
            await ThrowApiExceptionAsync(r);
        return await r.Content.ReadFromJsonAsync<T>();
    }

    protected async Task DeleteAsync(string url)
    {
        var r = await Http.DeleteAsync(url);
        if (!r.IsSuccessStatusCode)
            await ThrowApiExceptionAsync(r);
    }

    /// <summary>
    /// Reads the server's JSON error response ({"Message": "..."}) and throws an
    /// exception with the human-readable message rather than a generic HTTP status.
    /// </summary>
    private static async Task ThrowApiExceptionAsync(HttpResponseMessage r)
    {
        string message;
        try
        {
            var json = await r.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            // Try common server error shapes: {"Message":...} or {"message":...} or {"errors":...}
            if (doc.RootElement.TryGetProperty("Message", out var msg) ||
                doc.RootElement.TryGetProperty("message", out msg))
                message = msg.GetString() ?? r.ReasonPhrase ?? r.StatusCode.ToString();
            else if (doc.RootElement.TryGetProperty("Errors", out var errs) && errs.ValueKind == JsonValueKind.Array)
                message = string.Join(", ", errs.EnumerateArray().Select(e => e.GetString()));
            else
                message = $"HTTP {(int)r.StatusCode}: {r.ReasonPhrase}";
        }
        catch
        {
            message = $"HTTP {(int)r.StatusCode}: {r.ReasonPhrase}";
        }
        throw new HttpRequestException(message, null, r.StatusCode);
    }
}