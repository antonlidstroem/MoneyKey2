using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using MoneyKey.Core.DTOs.Auth;

namespace MoneyKey.Blazor.Services.Auth;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private static string?  _accessToken;
    private static UserDto? _currentUser;

    private readonly HttpClient _authClient;

    public JwtAuthenticationStateProvider(IConfiguration config)
    {
        var apiBase  = config["ApiBaseUrl"] ?? "https://localhost:7000";
        _authClient  = new HttpClient { BaseAddress = new Uri(apiBase) };
    }

    public static string?  AccessToken => _accessToken;
    public static UserDto? CurrentUser => _currentUser;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && !IsExpired(_accessToken))
            return Build(_accessToken);

        try
        {
            var r = await _authClient.PostAsync("api/auth/refresh", null);
            if (r.IsSuccessStatusCode)
            {
                var result = await r.Content.ReadFromJsonAsync<AuthResultDto>();
                if (result != null) { SetToken(result.AccessToken, result.User); return Build(result.AccessToken); }
            }
        }
        catch { }

        ClearToken();
        return Anonymous();
    }

    public void SetToken(string token, UserDto user)
    {
        _accessToken = token;
        _currentUser = user;
        NotifyAuthenticationStateChanged(Task.FromResult(Build(token)));
    }

    public void ClearToken()
    {
        _accessToken = null;
        _currentUser = null;
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));
    }

    private static AuthenticationState Build(string token)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var id  = new ClaimsIdentity(jwt.Claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(id));
        }
        catch { return Anonymous(); }
    }

    private static AuthenticationState Anonymous() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private static bool IsExpired(string token)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return jwt.ValidTo < DateTime.UtcNow.AddSeconds(-30);
        }
        catch { return true; }
    }
}
