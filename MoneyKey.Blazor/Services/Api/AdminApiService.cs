using System.Net.Http.Json;

namespace MoneyKey.Blazor.Services.Api;

/// <summary>
/// Client-side service for the superadmin SignalR toggle endpoint.
/// </summary>
public class AdminApiService : ApiServiceBase
{
    public AdminApiService(HttpClient http) : base(http) { }

    public async Task<bool> GetSignalRStatusAsync()
    {
        try
        {
            var r = await GetAsync<SignalRStatusResponse>("api/admin/signalr-status");
            return r?.Enabled ?? true;
        }
        catch { return true; }
    }

    public async Task<bool> ToggleSignalRAsync(bool enabled)
    {
        var r = await Http.PatchAsJsonAsync("api/admin/signalr-toggle", new { Enabled = enabled });
        r.EnsureSuccessStatusCode();
        var res = await r.Content.ReadFromJsonAsync<SignalRStatusResponse>();
        return res?.Enabled ?? enabled;
    }

    private class SignalRStatusResponse { public bool Enabled { get; set; } }
}
