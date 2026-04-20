using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using MoneyKey.Blazor.Services.Api;
using MoneyKey.Blazor.Services.Auth;

namespace MoneyKey.Blazor.State;

public class SignalRService : IAsyncDisposable
{
    private HubConnection? _hub;
    private int _budgetId;
    private string _apiBase = string.Empty;
    private bool _disabled = false;
    private AuthenticationStateProvider? _authProvider;

    public event Func<BudgetHubEvent, Task>? OnBudgetEvent;

    public async Task ConnectAsync(
        string apiBase,
        string accessToken,
        int budgetId,
        HttpClient http,
        AuthenticationStateProvider? authProvider = null)
    {
        _apiBase = apiBase;
        _authProvider = authProvider;

        try
        {
            var adminSvc = new AdminApiService(http);
            var status = await adminSvc.GetSignalRStatusAsync();
            _disabled = !(status?.Enabled ?? true);   // FIX: read .Enabled, then negate
        }
        catch { _disabled = false; }

        if (_disabled) return;

        if (_hub != null) { await _hub.DisposeAsync(); _hub = null; }

        _budgetId = budgetId;
        _hub = BuildConnection(apiBase, accessToken);
        RegisterHandlers();

        await _hub.StartAsync();
        await _hub.InvokeAsync("JoinBudget", budgetId);
    }

    public async Task SwitchBudgetAsync(int newBudgetId)
    {
        if (_disabled) return;
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.InvokeAsync("LeaveBudget", _budgetId);
            _budgetId = newBudgetId;
            await _hub.InvokeAsync("JoinBudget", newBudgetId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub != null) await _hub.DisposeAsync();
    }

    private HubConnection BuildConnection(string apiBase, string token) =>
        new HubConnectionBuilder()
            .WithUrl($"{apiBase}/hubs/budget?access_token={token}")
            .WithAutomaticReconnect(new TokenRefreshRetryPolicy(this))
            .Build();

    private void RegisterHandlers()
    {
        if (_hub == null) return;

        _hub.On<BudgetHubEvent>("BudgetEvent", async e =>
        {
            if (OnBudgetEvent != null) await OnBudgetEvent(e);
        });

        _hub.Reconnected += async _ =>
        {
            if (_hub?.State == HubConnectionState.Connected)
                await _hub.InvokeAsync("JoinBudget", _budgetId);
        };

        _hub.Closed += async ex =>
        {
            if (ex != null) await ReconnectWithFreshTokenAsync();
        };
    }

    internal async Task ReconnectWithFreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_apiBase) || _disabled) return;

        string? freshToken = null;
        if (_authProvider != null)
        {
            try
            {
                await _authProvider.GetAuthenticationStateAsync();
                freshToken = JwtAuthenticationStateProvider.AccessToken;
            }
            catch { }
        }

        if (string.IsNullOrWhiteSpace(freshToken)) return;

        try
        {
            if (_hub != null) { await _hub.DisposeAsync(); _hub = null; }
            _hub = BuildConnection(_apiBase, freshToken);
            RegisterHandlers();
            await _hub.StartAsync();
            await _hub.InvokeAsync("JoinBudget", _budgetId);
        }
        catch { }
    }

    private sealed class TokenRefreshRetryPolicy : IRetryPolicy
    {
        private readonly SignalRService _svc;
        private static readonly TimeSpan[] Delays =
            { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) };

        public TokenRefreshRetryPolicy(SignalRService svc) => _svc = svc;

        public TimeSpan? NextRetryDelay(RetryContext ctx)
        {
            if (ctx.PreviousRetryCount >= Delays.Length) return null;
            _ = _svc.ReconnectWithFreshTokenAsync();
            return Delays[ctx.PreviousRetryCount];
        }
    }
}