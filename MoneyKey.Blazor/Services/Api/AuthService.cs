using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using MoneyKey.Blazor.Services.Auth;
using MoneyKey.Core.DTOs.Auth;

namespace MoneyKey.Blazor.Services.Api;

public class AuthService : ApiServiceBase
{
    private readonly JwtAuthenticationStateProvider _provider;

    public AuthService(HttpClient http, JwtAuthenticationStateProvider provider) : base(http)
        => _provider = provider;

    public async Task<AuthResultDto?> RegisterAsync(RegisterDto dto)
    {
        var r = await PostAsync<AuthResultDto>("api/auth/register", dto);
        if (r != null) _provider.SetToken(r.AccessToken, r.User);
        return r;
    }

    public async Task<AuthResultDto?> LoginAsync(LoginDto dto)
    {
        var r = await PostAsync<AuthResultDto>("api/auth/login", dto);
        if (r != null) _provider.SetToken(r.AccessToken, r.User);
        return r;
    }

    public async Task LogoutAsync()
    {
        try { await Http.PostAsync("api/auth/logout", null); } catch { }
        _provider.ClearToken();
    }

    public async Task<AuthResultDto?> AcceptInviteAsync(string token)
    {
        var response = await Http.PostAsJsonAsync("api/auth/accept-invite", new AcceptInviteDto(token));
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadFromJsonAsync<ApiError>();
            throw new Exception(err?.Message ?? "Kunde inte acceptera inbjudan.");
        }
        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>();
        if (result != null) _provider.SetToken(result.AccessToken, result.User);
        return result;
    }

    public UserDto? CurrentUser => JwtAuthenticationStateProvider.CurrentUser;

    private record ApiError(string Message);
}
