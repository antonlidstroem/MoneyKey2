using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.API.Filters;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.SickLeave;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Controllers;

[RequireFeature("SickLeave")]
[Authorize, Route("api/budgets/{budgetId:int}/sickleave")]
public class SickLeaveController : BaseApiController
{
    private readonly ISickLeaveRepository _repo;
    private readonly BudgetAuthorizationService _auth;

    public SickLeaveController(ISickLeaveRepository repo, BudgetAuthorizationService auth)
    { _repo = repo; _auth = auth; }

    [HttpGet]
    public async Task<IActionResult> GetAll(int budgetId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var entries = await _repo.GetForBudgetAsync(budgetId, UserId);
        return Ok(entries.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int budgetId, [FromBody] CreateSickLeaveDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        if (dto.EndDate < dto.StartDate)
            return BadRequest(new { Message = "Slutdatum måste vara efter startdatum." });

        var entry = new SickLeaveEntry
        {
            BudgetId = budgetId,
            UserId = UserId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            SickLeaveType = dto.SickLeaveType,
            AnnualSgi = dto.AnnualSgi,
            GrossMonthlySalary = dto.GrossMonthlySalary,
            Notes = dto.Notes
        };
        return Ok(ToDto(await _repo.CreateAsync(entry)));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int budgetId, int id, [FromBody] CreateSickLeaveDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        if (dto.EndDate < dto.StartDate)
            return BadRequest(new { Message = "Slutdatum måste vara efter startdatum." });

        var entry = await _repo.GetByIdAsync(id, budgetId);
        if (entry == null) return NotFound();

        // Users may only edit their own sick leave records
        if (entry.UserId != UserId) return Forbid();

        entry.StartDate = dto.StartDate;
        entry.EndDate = dto.EndDate;
        entry.SickLeaveType = dto.SickLeaveType;
        entry.AnnualSgi = dto.AnnualSgi;
        entry.GrossMonthlySalary = dto.GrossMonthlySalary;
        entry.Notes = dto.Notes;

        return Ok(ToDto(await _repo.UpdateAsync(entry)));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int budgetId, int id)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var e = await _repo.GetByIdAsync(id, budgetId);
        if (e == null || e.UserId != UserId) return Forbid();
        await _repo.DeleteAsync(id, budgetId);
        return NoContent();
    }

    private static SickLeaveDto ToDto(SickLeaveEntry e) => new()
    {
        Id = e.Id,
        StartDate = e.StartDate,
        EndDate = e.EndDate,
        SickLeaveType = e.SickLeaveType,
        TotalDays = e.TotalDays,
        KarensDays = e.KarensDays,
        EmployerDays = e.EmployerDays,
        FkDays = e.FkDays,
        AnnualSgi = e.AnnualSgi,
        GrossMonthlySalary = e.GrossMonthlySalary,
        EmployerSickPay = e.EmployerSickPay,
        FkSickPay = e.FkSickPay,
        TotalBenefit = e.TotalBenefit,
        LostIncome = e.LostIncome,
        Notes = e.Notes,
        LinkedTransactionId = e.LinkedTransactionId
    };
}