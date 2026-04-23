using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using MoneyKey.Core.DTOs.Auth;

namespace MoneyKey.Blazor.Services.Auth;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private static string? _accessToken;
    private static UserDto? _currentUser;

    /// <summary>
    /// Dedicated HTTP client for auth endpoints (login, refresh, logout).
    /// This client does NOT use AuthorizationMessageHandler to avoid
    /// circular refresh loops when the access token is expired.
    /// It is registered as "MoneyKeyAuth" in Program.cs without the handler.
    /// </summary>
    private readonly HttpClient _authClient;

    public JwtAuthenticationStateProvider(IHttpClientFactory httpClientFactory)
    {
        // "MoneyKeyAuth" is a named client registered without AuthorizationMessageHandler.
        // It shares the same BaseAddress as the main API client but sends credentials
        // (cookies) correctly for cross-origin requests in Blazor WASM.
        _authClient = httpClientFactory.CreateClient("MoneyKeyAuth");
    }

    public static string? AccessToken => _accessToken;
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
                if (result != null)
                {
                    SetToken(result.AccessToken, result.User);
                    return Build(result.AccessToken);
                }
            }
        }
        catch { /* Network error — fall through to anonymous */ }

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
            var id = new ClaimsIdentity(jwt.Claims, "jwt");
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

    public UserDto? GetCurrentUser() => _currentUser;

    public Task LogoutAsync()
    {
        ClearToken();
        return Task.CompletedTask;
    }
}