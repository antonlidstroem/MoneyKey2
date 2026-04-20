using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.BudgetTarget;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Controllers;

[Authorize, Route("api/budgets/{budgetId:int}/targets")]
public class BudgetTargetsController : BaseApiController
{
    private readonly IBudgetTargetRepository    _repo;
    private readonly ITransactionRepository     _txRepo;
    private readonly BudgetAuthorizationService _auth;
    public BudgetTargetsController(IBudgetTargetRepository repo, ITransactionRepository txRepo, BudgetAuthorizationService auth)
    { _repo = repo; _txRepo = txRepo; _auth = auth; }

    [HttpGet]
    public async Task<IActionResult> GetAll(int budgetId, [FromQuery] int year = 0, [FromQuery] int month = 0)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        year  = year  == 0 ? DateTime.Today.Year  : year;
        month = month == 0 ? DateTime.Today.Month : month;
        var targets = await _repo.GetForMonthAsync(budgetId, year, month);
        // Compute actuals from transactions
        var from = new DateTime(year, month, 1);
        var to   = from.AddMonths(1).AddDays(-1);
        var q    = new MoneyKey.DAL.Queries.TransactionQuery { BudgetId = budgetId, StartDate = from, EndDate = to, ExcludeLinked = true };
        var txs  = await _txRepo.GetPagedAsync(q);
        var actuals = txs.Items.GroupBy(t => t.CategoryId).ToDictionary(g => g.Key, g => g.Sum(t => t.NetAmount));
        return Ok(targets.Select(t => new BudgetTargetDto
        {
            Id = t.Id, CategoryId = t.CategoryId, CategoryName = t.Category?.Name ?? "",
            Year = t.Year, Month = t.Month, TargetAmount = t.TargetAmount,
            ActualAmount = actuals.GetValueOrDefault(t.CategoryId, 0)
        }));
    }

    [HttpPut]
    public async Task<IActionResult> Upsert(int budgetId, [FromBody] UpsertBudgetTargetDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var t = new BudgetTarget { BudgetId = budgetId, CategoryId = dto.CategoryId, Year = dto.Year, Month = dto.Month, TargetAmount = dto.TargetAmount, Notes = dto.Notes };
        await _repo.UpsertAsync(t);
        return Ok(new { dto.CategoryId, dto.Year, dto.Month, dto.TargetAmount });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int budgetId, int id)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        await _repo.DeleteAsync(id, budgetId); return NoContent();
    }
}
