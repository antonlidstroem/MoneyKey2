using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MoneyKey.API.Hubs;

[Authorize]
public class BudgetHub : Hub
{
    public async Task JoinBudget(int budgetId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(budgetId));
        await Clients.Caller.SendAsync("JoinedBudget", budgetId);
    }

    public async Task LeaveBudget(int budgetId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(budgetId));

    public static string GroupName(int budgetId) => $"budget-{budgetId}";
}
