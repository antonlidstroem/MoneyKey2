using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoneyKey.API.Hubs;
using MoneyKey.API.Services;

namespace MoneyKey.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected string UserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException();

    protected string UserEmail =>
        User.FindFirst(ClaimTypes.Email)?.Value
        ?? User.FindFirst("email")?.Value
        ?? string.Empty;

    /// <summary>
    /// Broadcasts a budget event via SignalR only when the feature is enabled.
    /// Checking the toggle here keeps all controllers unaware of the feature flag.
    /// </summary>
    protected async Task BroadcastAsync(
        IHubContext<BudgetHub> hub,
        SignalRFeatureService signalRFeature,
        int budgetId,
        string evt,
        int? entityId = null)
    {
        if (!await signalRFeature.IsEnabledAsync()) return;

        await hub.Clients.Group(BudgetHub.GroupName(budgetId))
            .SendAsync("BudgetEvent", new BudgetEvent(evt, budgetId, entityId, UserEmail, DateTime.UtcNow));
    }
}
