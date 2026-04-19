using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.Core.DTOs.Subscription;
using MoneyKey.DAL.Models;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Constants;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Controllers;

[Authorize, Route("api/subscription")]
public class SubscriptionController : BaseApiController
{
    private readonly IUserSubscriptionRepository _subs;
    private readonly IBudgetRepository           _budgets;
    private readonly UserManager<ApplicationUser> _users;

    public SubscriptionController(IUserSubscriptionRepository subs, IBudgetRepository budgets,
        UserManager<ApplicationUser> users)
    { _subs = subs; _budgets = budgets; _users = users; }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine()
    {
        var sub  = await _subs.GetAsync(UserId) ?? new UserSubscription { UserId = UserId };
        var user = await _users.FindByIdAsync(UserId);
        var budgetCount = (await _budgets.GetForUserAsync(UserId)).Count;
        return Ok(ToDto(sub, user?.DisplayName ?? "", budgetCount));
    }

    [HttpPatch("display-name")]
    public async Task<IActionResult> SetDisplayName([FromBody] SetDisplayNameDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DisplayName) || dto.DisplayName.Length > 50)
            return BadRequest(new { Message = "Smeknamnet måste vara 1–50 tecken." });

        var existing = await _subs.FindUserIdByDisplayNameAsync(dto.DisplayName);
        if (existing != null && existing != UserId)
            return Conflict(new { Message = "Det smeknamnet är redan taget." });

        var user = await _users.FindByIdAsync(UserId);
        if (user == null) return NotFound();
        user.DisplayName = dto.DisplayName.Trim();
        await _users.UpdateAsync(user);
        return Ok(new { DisplayName = user.DisplayName });
    }

    [HttpGet("search-users")]
    public async Task<IActionResult> SearchUsers([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(Array.Empty<object>());
        var results = await _subs.SearchByDisplayNameAsync(q.Trim(), 8);
        // Never expose user IDs or emails to clients
        return Ok(results.Select(r => new { r.DisplayName }));
    }

    public static SubscriptionDto ToDto(UserSubscription sub, string displayName, int usedBudgets) => new()
    {
        UserId      = sub.UserId,
        Tier        = sub.Tier,
        TierLabel   = SubscriptionLimits.TierLabel(sub.Tier),
        PaidUntil   = sub.PaidUntil,
        IsActive    = sub.IsActive,
        IsAdmin     = sub.IsAdmin,
        MaxBudgets  = sub.IsAdmin ? int.MaxValue : SubscriptionLimits.MaxBudgets(sub.Tier),
        MaxMembers  = sub.IsAdmin ? int.MaxValue : SubscriptionLimits.MaxMembersPerBudget(sub.Tier),
        UsedBudgets = usedBudgets,
        DisplayName = displayName
    };
}

public record SetDisplayNameDto(string DisplayName);
