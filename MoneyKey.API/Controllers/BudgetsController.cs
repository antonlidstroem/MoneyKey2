using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.API.Services;
using MoneyKey.API.Services.Email;
using MoneyKey.Core.DTOs.Budget;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Controllers;

[Authorize, Route("api/budgets")]
public class BudgetsController : BaseApiController
{
    private readonly IBudgetRepository        _repo;
    private readonly ICategoryRepository      _cats;
    private readonly BudgetAuthorizationService _auth;
    private readonly IEmailService            _email;
    private readonly IConfiguration           _cfg;

    public BudgetsController(IBudgetRepository repo, ICategoryRepository cats,
        BudgetAuthorizationService auth, IEmailService email, IConfiguration cfg,
        IUserSubscriptionRepository subRepo)
    {
        _repo = repo; _cats = cats; _auth = auth; _email = email; _cfg = cfg; _subRepo = subRepo;
    }
    private readonly IUserSubscriptionRepository _subRepo;

    [HttpGet]
    public async Task<IActionResult> GetMyBudgets()
    {
        var budgets = await _repo.GetForUserAsync(UserId);
        var result  = new List<BudgetDto>();
        foreach (var b in budgets)
        {
            var m = await _auth.GetMembershipAsync(b.Id, UserId);
            result.Add(new BudgetDto(b.Id, b.Name, b.Description, b.IsActive, b.CreatedAt, m?.Role ?? BudgetMemberRole.Viewer));
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetDto dto)
    {
        // Enforce subscription budget limit
        var sub       = await _subRepo.GetAsync(UserId) ?? new() { UserId = UserId };
        var existing   = await _repo.GetForUserAsync(UserId);
        // B3 fix: only count active budgets the user owns (not just has membership in)
        var ownedActive = existing.Count(b => b.IsActive && b.OwnerId == UserId);
        var maxBudgets  = sub.IsAdmin ? int.MaxValue : SubscriptionLimits.MaxBudgets(sub.Tier);
        if (ownedActive >= maxBudgets)
            return BadRequest(new { Message = $"Din plan ({SubscriptionLimits.TierLabel(sub.Tier)}) tillåter max {maxBudgets} budgetar. Uppgradera för att skapa fler." });

        var budget = await _repo.CreateAsync(new Budget { Name = dto.Name, Description = dto.Description, OwnerId = UserId });
        await _repo.AddMemberAsync(new BudgetMembership { BudgetId = budget.Id, UserId = UserId, Role = BudgetMemberRole.Owner, AcceptedAt = DateTime.UtcNow });
        return CreatedAtAction(nameof(GetMyBudgets), new BudgetDto(budget.Id, budget.Name, budget.Description, budget.IsActive, budget.CreatedAt, BudgetMemberRole.Owner));
    }

    [HttpPut("{budgetId:int}")]
    public async Task<IActionResult> Update(int budgetId, [FromBody] UpdateBudgetDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Owner)) return Forbid();
        var b = await _repo.GetByIdAsync(budgetId);
        if (b == null) return NotFound();
        b.Name = dto.Name; b.Description = dto.Description;
        await _repo.UpdateAsync(b);
        return Ok();
    }

    [HttpDelete("{budgetId:int}")]
    public async Task<IActionResult> Delete(int budgetId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Owner)) return Forbid();
        await _repo.DeleteAsync(budgetId);
        return NoContent();
    }

    [HttpGet("{budgetId:int}/members")]
    public async Task<IActionResult> GetMembers(int budgetId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var b = await _repo.GetByIdAsync(budgetId);
        if (b == null) return NotFound();

        var result = b.Memberships
            .Where(m => m.AcceptedAt != null)
            .Select(m => new { m.Id, m.BudgetId, m.UserId, m.Role, m.InvitedByUserId, m.AcceptedAt })
            .ToList();

        return Ok(result);
    }

    [HttpPost("{budgetId:int}/invite")]
    public async Task<IActionResult> Invite(int budgetId, [FromBody] InviteMemberDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Owner)) return Forbid();

        // FIX CRITICAL-2: Check for an existing pending invite for this email on this budget.
        // BudgetMembership stores the invitee's email as a placeholder in UserId until they accept.
        // The UNIQUE index on (BudgetId, UserId) means a second invite to the same email would fail.
        var existing = await _repo.GetPendingInviteAsync(budgetId, dto.Email);
        if (existing != null)
        {
            // Replace the old invite token so the old link is invalidated.
            existing.InviteToken     = Guid.NewGuid().ToString("N");
            existing.Role            = dto.Role;
            existing.InvitedByUserId = UserId;
            existing.InvitedAt       = DateTime.UtcNow;
            await _repo.UpdateMemberAsync(existing);
            var baseUrl2 = _cfg["AppBaseUrl"] ?? "https://localhost:7001";
            var b2       = await _repo.GetByIdAsync(budgetId);
            await _email.SendInviteAsync(dto.Email, b2?.Name ?? "Budget", existing.InviteToken, baseUrl2);
            return Ok(new { Message = "Ny inbjudan skickad (tidigare inbjudan ersatt)." });
        }

        var token = Guid.NewGuid().ToString("N");
        // Note: UserId is temporarily set to the invitee email as a placeholder.
        // AcceptInvite overwrites it with the real IdentityUser ID.
        await _repo.AddMemberAsync(new BudgetMembership
        {
            BudgetId = budgetId, UserId = dto.Email, Role = dto.Role,
            InviteToken = token, InvitedByUserId = UserId
        });
        var baseUrl = _cfg["AppBaseUrl"] ?? "https://localhost:7001";
        var b       = await _repo.GetByIdAsync(budgetId);
        await _email.SendInviteAsync(dto.Email, b?.Name ?? "Budget", token, baseUrl);
        return Ok(new { Message = "Inbjudan skickad." });
    }

    [HttpGet("{budgetId:int}/categories")]
    public async Task<IActionResult> GetCategories(int budgetId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        return Ok(await _cats.GetForBudgetAsync(budgetId));
    }

    // ── Feature flags ─────────────────────────────────────────────────────────

    /// <summary>Returns the list of feature keys that are DISABLED for this budget.</summary>
    [HttpGet("{budgetId:int}/features/disabled")]
    public async Task<IActionResult> GetDisabledFeatures(int budgetId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var keys = await _repo.GetDisabledFeaturesAsync(budgetId);
        return Ok(keys);
    }

    [HttpPatch("{budgetId:int}/features/{feature}")]
    public async Task<IActionResult> SetFeature(int budgetId, string feature, [FromBody] SetFeatureDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Owner)) return Forbid();
        if (dto.Enabled)
            await _repo.EnableFeatureAsync(budgetId, feature);
        else
            await _repo.DisableFeatureAsync(budgetId, feature);
        return Ok();
    }
}
