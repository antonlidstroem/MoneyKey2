using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoneyKey.API.Hubs;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.Milersattning;
using MoneyKey.Core.Services;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;

namespace MoneyKey.API.Controllers;

[Authorize, Route("api/budgets/{budgetId:int}/milersattning")]
public class MilersattningController : BaseApiController
{
    /// <summary>Cross-budget view: returns entries for all supplied budgetIds the caller has access to.</summary>
    [HttpGet("~/api/milersattning/cross-budget")]
    public async Task<IActionResult> GetCrossBudget([FromQuery] int[] budgetIds)
    {
        var all = new List<object>();
        foreach (var bid in budgetIds.Distinct())
        {
            if (!await _auth.HasRoleAsync(bid, UserId, BudgetMemberRole.Viewer)) continue;
            var entries = await _repo.GetForBudgetAsync(bid);

           
            all.AddRange(entries.Select(e =>
                (object)MilersattningService.ToDto(e, MilersattningService.SwedishStatus(e.Status))));
        }
        return Ok(all);
    }

    private readonly IMilersattningRepository   _repo;
    private readonly MilersattningService       _svc;
    private readonly BudgetAuthorizationService _auth;
    private readonly IHubContext<BudgetHub>     _hub;
    private readonly SignalRFeatureService      _signalRFeature;

    public MilersattningController(IMilersattningRepository repo, MilersattningService svc,
        BudgetAuthorizationService auth, IHubContext<BudgetHub> hub, SignalRFeatureService signalRFeature)
    { _repo = repo; _svc = svc; _auth = auth; _hub = hub; _signalRFeature = signalRFeature; }

    [HttpGet]
    public async Task<IActionResult> GetAll(int budgetId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var items = await _repo.GetForBudgetAsync(budgetId);
        return Ok(items.Select(m => MilersattningService.ToDto(m, MilersattningService.SwedishStatus(m.Status))));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int budgetId, [FromBody] CreateMilersattningDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var entry = await _svc.CreateAsync(budgetId, UserId, dto);
        await BroadcastAsync(_hub, _signalRFeature, budgetId, "MilersattningCreated", entry.Id);
        return Ok(MilersattningService.ToDto(entry, MilersattningService.SwedishStatus(entry.Status)));
    }

    [HttpPatch("{entryId:int}/status")]
    public async Task<IActionResult> UpdateStatus(int budgetId, int entryId, [FromBody] UpdateMilersattningStatusDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var entry = await _svc.UpdateStatusAsync(entryId, budgetId, dto.Status);
        if (entry == null) return NotFound();
        await BroadcastAsync(_hub, _signalRFeature, budgetId, "MilersattningUpdated", entryId);
        return Ok(MilersattningService.ToDto(entry, MilersattningService.SwedishStatus(entry.Status)));
    }

    [HttpDelete("{entryId:int}")]
    public async Task<IActionResult> Delete(int budgetId, int entryId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        await _svc.DeleteAsync(entryId, budgetId);
        await BroadcastAsync(_hub, _signalRFeature, budgetId, "MilersattningDeleted", entryId);
        return NoContent();
    }

    [HttpGet("rate")]
    public async Task<IActionResult> GetRate(int budgetId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        return Ok(new { Rate = await _svc.GetRateAsync(budgetId), Standard = MilersattningService.SkatteverketStandardRate });
    }
}
