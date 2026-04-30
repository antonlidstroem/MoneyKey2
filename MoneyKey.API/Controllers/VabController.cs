using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoneyKey.API.Filters;
using MoneyKey.API.Hubs;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.Vab;
using MoneyKey.Core.Services;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;

namespace MoneyKey.API.Controllers;

[RequireFeature("Vab")]
[Authorize, Route("api/budgets/{budgetId:int}/vab")]
public class VabController : BaseApiController
{
    [HttpGet("~/api/vab/cross-budget")]
    public async Task<IActionResult> GetCrossBudget([FromQuery] int[] budgetIds)
    {
        var all = new List<object>();
        foreach (var bid in budgetIds.Distinct())
        {
            if (!await _auth.HasRoleAsync(bid, UserId, BudgetMemberRole.Viewer)) continue;
            var entries = await _repo.GetForBudgetAsync(bid);
            all.AddRange(entries.Select(e => (object)_svc.ToDto(e)));
        }
        return Ok(all);
    }

    private readonly IVabRepository             _repo;
    private readonly VabService                 _svc;
    private readonly BudgetAuthorizationService _auth;
    private readonly IHubContext<BudgetHub>     _hub;
    private readonly SignalRFeatureService      _signalRFeature;

    public VabController(IVabRepository repo, VabService svc,
        BudgetAuthorizationService auth, IHubContext<BudgetHub> hub, SignalRFeatureService signalRFeature)
    { _repo = repo; _svc = svc; _auth = auth; _hub = hub; _signalRFeature = signalRFeature; }

    [HttpGet]
    public async Task<IActionResult> GetAll(int budgetId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var items = await _repo.GetForBudgetAsync(budgetId);
        return Ok(items.Select(v => new VabDto
        {
            Id = v.Id, BudgetId = v.BudgetId, UserId = v.UserId, ChildName = v.ChildName,
            StartDate = v.StartDate, EndDate = v.EndDate, DailyBenefit = v.DailyBenefit,
            Rate = v.Rate, TotalDays = v.TotalDays, TotalAmount = v.TotalAmount,
            LinkedTransactionId = v.LinkedTransactionId
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int budgetId, [FromBody] CreateVabDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var entry = await _svc.CreateAsync(budgetId, UserId, dto);
        await BroadcastAsync(_hub, _signalRFeature, budgetId, "VabCreated", entry.Id);
        return Ok(entry);
    }

    [HttpDelete("{entryId:int}")]
    public async Task<IActionResult> Delete(int budgetId, int entryId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        await _svc.DeleteAsync(entryId, budgetId);
        await BroadcastAsync(_hub, _signalRFeature, budgetId, "VabDeleted", entryId);
        return NoContent();
    }
}
