using System.Net.Http.Json;
using MoneyKey.Core.DTOs.Subscription;

namespace MoneyKey.Blazor.Services.Api;

public class AdminApiService : ApiServiceBase
{
    public AdminApiService(HttpClient http) : base(http) { }

    public Task<AdminUserListResult?> GetUsersAsync(string? search = null, int page = 1) =>
        GetAsync<AdminUserListResult>($"api/admin/users?page={page}{(search != null ? $"&search={Uri.EscapeDataString(search)}" : "")}");

    public async Task<SubscriptionDto?> UpdateSubscriptionAsync(string userId, UpdateSubscriptionDto dto)
    {
        var r = await Http.PatchAsJsonAsync($"api/admin/users/{userId}/subscription", dto);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<SubscriptionDto>();
    }

    public Task<AdminStatsResult?> GetStatsAsync() =>
        GetAsync<AdminStatsResult>("api/admin/stats");

    public Task<SignalRStatusResult?> GetSignalRStatusAsync() =>
        GetAsync<SignalRStatusResult>("api/admin/signalr-status");

    public async Task ToggleSignalRAsync(bool enabled)
    {
        var r = await Http.PatchAsJsonAsync("api/admin/signalr-toggle", new { Enabled = enabled });
        r.EnsureSuccessStatusCode();
    }
}

public class AdminUserListResult
{
    public List<AdminUserDto> Users { get; set; } = new();
    public int                Total { get; set; }
    public int                Page  { get; set; }
}

public class AdminStatsResult
{
    public int                        Total      { get; set; }
    public Dictionary<string, int>    TierCounts { get; set; } = new();
    public int                        Paid       { get; set; }
    public int                        Expired    { get; set; }
    public int                        Admins     { get; set; }
    public bool                       SignalREnabled { get; set; }
}

public class SignalRStatusResult { public bool Enabled { get; set; } }
