using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.API.Filters;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.Csn;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Controllers;

[RequireFeature("Csn")]
[Authorize, Route("api/budgets/{budgetId:int}/csn")]
public class CsnController : BaseApiController
{
    private readonly ICsnRepository _repo;
    private readonly BudgetAuthorizationService _auth;

    public CsnController(ICsnRepository repo, BudgetAuthorizationService auth)
    { _repo = repo; _auth = auth; }

    [HttpGet]
    public async Task<IActionResult> GetAll(int budgetId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var entries = await _repo.GetForBudgetAsync(budgetId, UserId);
        return Ok(entries.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int budgetId, [FromBody] CreateCsnDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();

        var existing = await _repo.GetForBudgetAsync(budgetId, UserId);
        if (existing.Any(e => e.Year == dto.Year))
            return BadRequest(new { Message = $"Det finns redan en CSN-post för år {dto.Year}." });

        return Ok(ToDto(await _repo.CreateAsync(Map(dto, budgetId))));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int budgetId, int id, [FromBody] CreateCsnDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var entry = await _repo.GetByIdAsync(id, budgetId);
        if (entry == null || entry.UserId != UserId) return NotFound();

        entry.Year = dto.Year; entry.TotalOriginalDebt = dto.TotalOriginalDebt;
        entry.CurrentBalance = dto.CurrentBalance; entry.AnnualRepayment = dto.AnnualRepayment;
        entry.AnnualIncomeLimit = dto.AnnualIncomeLimit;
        entry.EstimatedAnnualIncome = dto.EstimatedAnnualIncome;
        entry.IsCurrentlyStudying = dto.IsCurrentlyStudying;
        entry.MonthlyStudyGrant = dto.MonthlyStudyGrant;
        entry.MonthlyStudyLoan = dto.MonthlyStudyLoan;
        entry.Notes = dto.Notes;

        return Ok(ToDto(await _repo.UpdateAsync(entry)));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int budgetId, int id)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var entry = await _repo.GetByIdAsync(id, budgetId);
        if (entry == null || entry.UserId != UserId) return Forbid();
        await _repo.DeleteAsync(id, budgetId);
        return NoContent();
    }

    private CsnEntry Map(CreateCsnDto dto, int budgetId) => new()
    {
        BudgetId = budgetId,
        UserId = UserId,
        Year = dto.Year,
        TotalOriginalDebt = dto.TotalOriginalDebt,
        CurrentBalance = dto.CurrentBalance,
        AnnualRepayment = dto.AnnualRepayment,
        AnnualIncomeLimit = dto.AnnualIncomeLimit,
        EstimatedAnnualIncome = dto.EstimatedAnnualIncome,
        IsCurrentlyStudying = dto.IsCurrentlyStudying,
        MonthlyStudyGrant = dto.MonthlyStudyGrant,
        MonthlyStudyLoan = dto.MonthlyStudyLoan,
        Notes = dto.Notes
    };

    private static CsnDto ToDto(CsnEntry e) => new()
    {
        Id = e.Id,
        Year = e.Year,
        TotalOriginalDebt = e.TotalOriginalDebt,
        CurrentBalance = e.CurrentBalance,
        AnnualRepayment = e.AnnualRepayment,
        MonthlyRepayment = e.MonthlyRepayment,
        AnnualIncomeLimit = e.AnnualIncomeLimit,
        EstimatedAnnualIncome = e.EstimatedAnnualIncome,
        IncomeMargin = e.IncomeMargin,
        YearsRemaining = e.YearsRemaining,
        IsOverIncomeLimit = e.IsOverIncomeLimit,
        IsCurrentlyStudying = e.IsCurrentlyStudying,
        MonthlyStudyGrant = e.MonthlyStudyGrant,
        MonthlyStudyLoan = e.MonthlyStudyLoan,
        Notes = e.Notes
    };
}