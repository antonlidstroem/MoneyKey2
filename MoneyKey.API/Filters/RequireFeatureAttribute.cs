using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;

namespace MoneyKey.API.Filters;

/// <summary>
/// Checks whether the feature is enabled for the budget identified in the route.
/// Returns 403 if the feature has been disabled via Settings → Funktioner.
/// Apply at controller class level: [RequireFeature("Vab")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireFeatureAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _featureKey;

    public RequireFeatureAttribute(string featureKey) => _featureKey = featureKey;

    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        // Only enforce when the route contains a budgetId
        if (!ctx.RouteData.Values.TryGetValue("budgetId", out var raw) ||
            !int.TryParse(raw?.ToString(), out var budgetId))
        {
            await next(); return;
        }

        var db = ctx.HttpContext.RequestServices.GetRequiredService<BudgetDbContext>();
        var key = $"Feature_{_featureKey}";

        var isDisabled = await db.AppSettings
            .AnyAsync(s => s.BudgetId == budgetId && s.Key == key && s.Value == "disabled");

        if (isDisabled)
        {
            ctx.Result = new ObjectResult(
                new { Message = $"Funktionen '{_featureKey}' är inaktiverad för denna budget." })
            { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        await next();
    }
}