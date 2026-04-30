using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.BudgetTarget;
using MoneyKey.DAL.Data;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Controllers;

[Authorize, Route("api/budgets/{budgetId:int}/targets")]
public class BudgetTargetsController : BaseApiController
{
    private readonly BudgetDbContext _db;
    private readonly BudgetAuthorizationService _auth;

    public BudgetTargetsController(BudgetDbContext db, BudgetAuthorizationService auth)
    { _db = db; _auth = auth; }

    /// <summary>
    /// Returns targets for a month, optionally enriched with actual spending.
    /// Query: ?year=2025&month=4&includeActuals=true
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetForMonth(
        int budgetId,
        [FromQuery] int year = 0,
        [FromQuery] int month = 0,
        [FromQuery] bool includeActuals = false)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();

        if (year == 0) year = DateTime.Today.Year;
        if (month == 0) month = DateTime.Today.Month;

        var targets = await _db.BudgetTargets
            .Include(t => t.Category)
            .Where(t => t.BudgetId == budgetId && t.Year == year && t.Month == month)
            .ToListAsync();

        // Optionally include actual spending per category for the same month
        Dictionary<int, decimal> actuals = new();
        if (includeActuals)
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);

            actuals = await _db.Transactions
                .Where(t => t.BudgetId == budgetId
                         && t.StartDate >= from && t.StartDate <= to
                         && t.Type == TransactionType.Expense && t.IsActive)
                .GroupBy(t => t.CategoryId)
                .Select(g => new { g.Key, Sum = g.Sum(t => t.NetAmount) })
                .ToDictionaryAsync(x => x.Key, x => x.Sum);
        }

        return Ok(targets.Select(t => ToDto(t, actuals)));
    }

    /// <summary>
    /// Upsert: skapar eller uppdaterar target för kategori+år+månad.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Upsert(int budgetId, [FromBody] UpsertBudgetTargetDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        if (dto.TargetAmount < 0) return BadRequest(new { Message = "Beloppet kan inte vara negativt." });

        var existing = await _db.BudgetTargets
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t =>
                t.BudgetId == budgetId &&
                t.CategoryId == dto.CategoryId &&
                t.Year == dto.Year &&
                t.Month == dto.Month);

        if (existing != null)
        {
            existing.TargetAmount = dto.TargetAmount;
        }
        else
        {
            existing = new BudgetTarget
            {
                BudgetId = budgetId,
                CategoryId = dto.CategoryId,
                TargetAmount = dto.TargetAmount,
                Year = dto.Year,
                Month = dto.Month
            };
            _db.BudgetTargets.Add(existing);
        }

        await _db.SaveChangesAsync();

        // Reload with Category navigation
        await _db.Entry(existing).Reference(t => t.Category).LoadAsync();
        return Ok(ToDto(existing, new()));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int budgetId, int id)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var t = await _db.BudgetTargets.FirstOrDefaultAsync(x => x.Id == id && x.BudgetId == budgetId);
        if (t == null) return NotFound();
        _db.BudgetTargets.Remove(t);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static BudgetTargetDto ToDto(BudgetTarget t, Dictionary<int, decimal> actuals) => new()
    {
        Id = t.Id,
        CategoryId = t.CategoryId,
        CategoryName = t.Category?.Name ?? $"Kategori {t.CategoryId}",
        TargetAmount = t.TargetAmount,
        Year = t.Year,
        Month = t.Month,
        ActualAmount = actuals.TryGetValue(t.CategoryId, out var a) ? a : null
    };
}