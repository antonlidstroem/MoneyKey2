using System.Net.Http.Json;
using MoneyKey.Core.DTOs.Invitation;
using MoneyKey.Domain.Enums;

namespace MoneyKey.Blazor.Services.Api;

public class InvitationApiService : ApiServiceBase
{
    public InvitationApiService(HttpClient http) : base(http) { }

    public Task<List<InvitationDto>?> GetPendingAsync() =>
        GetAsync<List<InvitationDto>>("api/invitations/pending");

    public async Task AcceptAsync(int id)
    {
        var r = await Http.PostAsync($"api/invitations/{id}/accept", null);
        r.EnsureSuccessStatusCode();
    }

    public async Task DeclineAsync(int id)
    {
        var r = await Http.PostAsync($"api/invitations/{id}/decline", null);
        r.EnsureSuccessStatusCode();
    }

    public async Task<string?> SendInvitationAsync(int budgetId, string displayName, BudgetMemberRole role)
    {
        var r = await Http.PostAsJsonAsync($"api/budgets/{budgetId}/invitations",
            new SendInvitationDto { DisplayName = displayName, Role = role });
        if (!r.IsSuccessStatusCode)
        {
            var err = await r.Content.ReadFromJsonAsync<ErrBody>();
            return err?.Message ?? "Fel.";
        }
        return null;
    }

    public async Task<string?> TransferOwnerAsync(int budgetId, string newOwnerDisplayName)
    {
        var r = await Http.PatchAsJsonAsync($"api/budgets/{budgetId}/transfer-owner",
            new { NewOwnerDisplayName = newOwnerDisplayName });
        if (!r.IsSuccessStatusCode)
        {
            var err = await r.Content.ReadFromJsonAsync<ErrBody>();
            return err?.Message ?? "Fel.";
        }
        return null;
    }

    private record ErrBody(string Message);
    private record SendInvitationDto { public string DisplayName { get; set; } = ""; public BudgetMemberRole Role { get; set; } }
}
