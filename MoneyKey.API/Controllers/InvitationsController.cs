using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.Core.DTOs.Invitation;
using MoneyKey.DAL.Models;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Constants;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Controllers;

[Authorize, Route("api")]
public class InvitationsController : BaseApiController
{
    private readonly IBudgetInvitationRepository _invRepo;
    private readonly IBudgetRepository           _budgetRepo;
    private readonly IUserSubscriptionRepository _subRepo;
    private readonly UserManager<ApplicationUser> _users;

    public InvitationsController(IBudgetInvitationRepository invRepo, IBudgetRepository budgetRepo,
        IUserSubscriptionRepository subRepo, UserManager<ApplicationUser> users)
    { _invRepo = invRepo; _budgetRepo = budgetRepo; _subRepo = subRepo; _users = users; }

    // ── My pending invitations ────────────────────────────────────────────────
    [HttpGet("invitations/pending")]
    public async Task<IActionResult> GetPending()
    {
        var invitations = await _invRepo.GetPendingForUserAsync(UserId);
        var dtos = new List<InvitationDto>();
        foreach (var inv in invitations)
        {
            var inviter = await _users.FindByIdAsync(inv.InvitedByUserId);
            dtos.Add(new InvitationDto
            {
                Id            = inv.Id,
                BudgetId      = inv.BudgetId,
                BudgetName    = inv.Budget.Name,
                InvitedByName = inviter?.DisplayName ?? "Okänd",
                Role          = inv.Role,
                Status        = inv.Status,
                CreatedAt     = inv.CreatedAt,
                ExpiresAt     = inv.ExpiresAt
            });
        }
        return Ok(dtos);
    }

    [HttpPost("invitations/{id:int}/accept")]
    public async Task<IActionResult> Accept(int id)
    {
        var inv = await _invRepo.GetByIdAsync(id);
        if (inv == null || inv.InvitedUserId != UserId) return NotFound();
        if (inv.Status != BudgetInvitationStatus.Pending || inv.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { Message = "Inbjudan är inte längre giltig." });

        await _budgetRepo.AddMemberAsync(new BudgetMembership
        {
            BudgetId = inv.BudgetId, UserId = UserId, Role = inv.Role,
            InvitedByUserId = inv.InvitedByUserId, AcceptedAt = DateTime.UtcNow
        });
        await _invRepo.UpdateStatusAsync(id, BudgetInvitationStatus.Accepted);
        return Ok(new { Message = "Inbjudan accepterad.", BudgetId = inv.BudgetId });
    }

    [HttpPost("invitations/{id:int}/decline")]
    public async Task<IActionResult> Decline(int id)
    {
        var inv = await _invRepo.GetByIdAsync(id);
        if (inv == null || inv.InvitedUserId != UserId) return NotFound();
        await _invRepo.UpdateStatusAsync(id, BudgetInvitationStatus.Declined);
        return Ok(new { Message = "Inbjudan avböjd." });
    }

    // ── Send invitation (from budget owner) ───────────────────────────────────
    [HttpPost("budgets/{budgetId:int}/invitations")]
    public async Task<IActionResult> SendInvitation(int budgetId, [FromBody] SendInvitationDto dto)
    {
        // Auth: only Owner can invite
        var membership = await _budgetRepo.GetMembershipAsync(budgetId, UserId);
        if (membership?.Role != BudgetMemberRole.Owner) return Forbid();

        // Find target user by DisplayName
        var targetUserId = await _subRepo.FindUserIdByDisplayNameAsync(dto.DisplayName);
        if (targetUserId == null)
            return NotFound(new { Message = $"Ingen användare med smeknamnet \"{dto.DisplayName}\" hittades." });
        if (targetUserId == UserId)
            return BadRequest(new { Message = "Du kan inte bjuda in dig själv." });

        // Check if already a member
        var existingMember = await _budgetRepo.GetMembershipAsync(budgetId, targetUserId);
        if (existingMember != null)
            return Conflict(new { Message = "Den här användaren är redan medlem i budgeten." });

        // Check owner's subscription limit
        var ownerSub    = await _subRepo.GetAsync(UserId) ?? new() { UserId = UserId };
        var memberCount = (await _budgetRepo.GetMembersAsync(budgetId)).Count;
        var maxMembers  = ownerSub.IsAdmin ? int.MaxValue : SubscriptionLimits.MaxMembersPerBudget(ownerSub.Tier);
        if (memberCount >= maxMembers)
            return BadRequest(new { Message = $"Din prenumerationsplan ({SubscriptionLimits.TierLabel(ownerSub.Tier)}) tillåter max {maxMembers} extra medlemmar. Uppgradera för att bjuda in fler." });

        // Cancel any existing pending invite for same user+budget
        if (await _invRepo.HasPendingAsync(budgetId, targetUserId))
            return Conflict(new { Message = "En väntande inbjudan finns redan för den användaren." });

        var inv = await _invRepo.CreateAsync(new BudgetInvitation
        {
            BudgetId = budgetId, InvitedByUserId = UserId,
            InvitedUserId = targetUserId, Role = dto.Role,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        return Ok(new { Message = $"Inbjudan skickad till {dto.DisplayName}.", InvitationId = inv.Id });
    }

    // ── Transfer ownership ────────────────────────────────────────────────────
    [HttpPatch("budgets/{budgetId:int}/transfer-owner")]
    public async Task<IActionResult> TransferOwner(int budgetId, [FromBody] TransferOwnerDto dto)
    {
        var ownerM = await _budgetRepo.GetMembershipAsync(budgetId, UserId);
        if (ownerM?.Role != BudgetMemberRole.Owner) return Forbid();

        var targetId  = await _subRepo.FindUserIdByDisplayNameAsync(dto.NewOwnerDisplayName);
        if (targetId == null) return NotFound(new { Message = "Användaren hittades inte." });

        var targetM = await _budgetRepo.GetMembershipAsync(budgetId, targetId);
        if (targetM == null) return BadRequest(new { Message = "Den nya ägaren måste redan vara medlem i budgeten." });

        // Swap roles
        ownerM.Role  = BudgetMemberRole.Editor;
        targetM.Role = BudgetMemberRole.Owner;
        var budget   = await _budgetRepo.GetByIdAsync(budgetId);
        if (budget != null) { budget.OwnerId = targetId; await _budgetRepo.UpdateAsync(budget); }
        await _budgetRepo.UpdateMemberAsync(ownerM);
        await _budgetRepo.UpdateMemberAsync(targetM);
        return Ok(new { Message = "Ägarskap överfört." });
    }
}

public record TransferOwnerDto(string NewOwnerDisplayName);
