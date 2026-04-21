using System.Net.Http.Json;
using MoneyKey.Core.DTOs.Subscription;

namespace MoneyKey.Blazor.Services.Api;

public class SubscriptionApiService : ApiServiceBase
{
    public SubscriptionApiService(HttpClient http) : base(http) { }

    public Task<SubscriptionDto?> GetMineAsync() =>
        GetAsync<SubscriptionDto>("api/subscription/me");

    public async Task<string?> SetDisplayNameAsync(string displayName)
    {
        var r = await Http.PatchAsJsonAsync("api/subscription/display-name", new { DisplayName = displayName });
        if (!r.IsSuccessStatusCode)
        {
            var err = await r.Content.ReadFromJsonAsync<ErrorResponse>();
            return err?.Message ?? "Fel vid uppdatering.";
        }
        return null; // null = success
    }

    public async Task<List<string>> SearchUsersAsync(string prefix)
    {
        var r = await GetAsync<List<DisplayNameResult>>($"api/subscription/search-users?q={Uri.EscapeDataString(prefix)}");
        return r?.Select(x => x.DisplayName).ToList() ?? new();
    }

    private record ErrorResponse(string Message);
    private record DisplayNameResult(string DisplayName);
}
