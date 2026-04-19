using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.Subscription;
using MoneyKey.DAL.Models;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Constants;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Controllers;

[Authorize, Route("api/admin")]
public class AdminController : BaseApiController
{
    private readonly SignalRFeatureService       _signalR;
    private readonly IUserSubscriptionRepository _subs;
    private readonly IBudgetRepository           _budgets;
    private readonly UserManager<ApplicationUser> _users;

    public AdminController(SignalRFeatureService signalR, IUserSubscriptionRepository subs,
        IBudgetRepository budgets, UserManager<ApplicationUser> users)
    { _signalR = signalR; _subs = subs; _budgets = budgets; _users = users; }

    // ── Guard ─────────────────────────────────────────────────────────────────
    private async Task<bool> IsAdminAsync()
    {
        var sub = await _subs.GetAsync(UserId);
        return sub?.IsAdmin == true;
    }

    // ── SignalR ───────────────────────────────────────────────────────────────
    [HttpGet("signalr-status")]
    public async Task<IActionResult> GetSignalRStatus()
    {
        if (!await IsAdminAsync()) return Forbid();
        return Ok(new { Enabled = await _signalR.IsEnabledAsync() });
    }

    [HttpPatch("signalr-toggle")]
    public async Task<IActionResult> ToggleSignalR([FromBody] SetSignalREnabledDto dto)
    {
        if (!await IsAdminAsync()) return Forbid();
        await _signalR.SetEnabledAsync(dto.Enabled);
        return Ok(new { Enabled = dto.Enabled });
    }

    // ── Users ─────────────────────────────────────────────────────────────────
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] int page = 1)
    {
        if (!await IsAdminAsync()) return Forbid();
        var rows  = await _subs.GetAllForAdminAsync(search, page, 50);
        var total = await _subs.CountAsync();
        var dtos  = new List<AdminUserDto>();
        foreach (var (userId, email, displayName, sub) in rows)
        {
            var user        = await _users.FindByIdAsync(userId);
            var budgetCount = (await _budgets.GetForUserAsync(userId)).Count;
            dtos.Add(new AdminUserDto
            {
                UserId      = userId,
                Email       = email,
                DisplayName = displayName,
                FirstName   = user?.FirstName,
                LastName    = user?.LastName,
                Tier        = sub.Tier,
                TierLabel   = SubscriptionLimits.TierLabel(sub.Tier),
                PaidUntil   = sub.PaidUntil,
                IsActive    = sub.IsActive,
                IsAdmin     = sub.IsAdmin,
                AdminNotes  = sub.AdminNotes,
                BudgetCount = budgetCount,
                CreatedAt   = user?.CreatedAt ?? DateTime.UtcNow
            });
        }
        return Ok(new { Users = dtos, Total = total, Page = page });
    }

    [HttpPatch("users/{userId}/subscription")]
    public async Task<IActionResult> UpdateSubscription(string userId, [FromBody] UpdateSubscriptionDto dto)
    {
        if (!await IsAdminAsync()) return Forbid();
        var sub = await _subs.UpsertAsync(new UserSubscription
        {
            UserId     = userId,
            Tier       = dto.Tier,
            PaidUntil  = dto.PaidUntil,
            PaymentRef = dto.PaymentRef,
            IsAdmin    = dto.IsAdmin,
            AdminNotes = dto.AdminNotes
        });
        var user = await _users.FindByIdAsync(userId);
        return Ok(SubscriptionController.ToDto(sub, user?.DisplayName ?? "", 0));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        if (!await IsAdminAsync()) return Forbid();
        var total    = await _subs.CountAsync();
        var all      = await _subs.GetAllForAdminAsync(pageSize: total > 0 ? total : 1);
        var tierCounts = all.GroupBy(r => r.Sub.Tier)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());
        var paid     = all.Count(r => r.Sub.PaidUntil != null && r.Sub.PaidUntil >= DateTime.UtcNow);
        var expired  = all.Count(r => r.Sub.PaidUntil != null && r.Sub.PaidUntil < DateTime.UtcNow);
        var admins   = all.Count(r => r.Sub.IsAdmin);
        return Ok(new { Total = total, TierCounts = tierCounts, Paid = paid, Expired = expired, Admins = admins, SignalREnabled = await _signalR.IsEnabledAsync() });
    }
}
