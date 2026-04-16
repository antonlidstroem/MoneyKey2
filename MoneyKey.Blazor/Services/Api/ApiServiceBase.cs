using System.Net.Http.Json;

namespace MoneyKey.Blazor.Services.Api;

public abstract class ApiServiceBase
{
    protected readonly HttpClient Http;
    protected ApiServiceBase(HttpClient http) => Http = http;

    protected Task<T?> GetAsync<T>(string url) => Http.GetFromJsonAsync<T>(url);

    protected async Task<T?> PostAsync<T>(string url, object body)
    {
        var r = await Http.PostAsJsonAsync(url, body);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<T>();
    }

    protected async Task DeleteAsync(string url)
    {
        var r = await Http.DeleteAsync(url);
        r.EnsureSuccessStatusCode();
    }
}
