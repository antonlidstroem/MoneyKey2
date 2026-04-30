using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.API.Filters;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.TimeEntry;
using MoneyKey.Core.Services;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;

namespace MoneyKey.API.Controllers;

[RequireFeature("TimeTracking")]
[Authorize, Route("api/budgets/{budgetId:int}/timeentries")]
public class TimeEntriesController : BaseApiController
{
    private readonly ITimeEntryRepository      _repo;
    private readonly IJobRepository            _jobs;
    private readonly TimeTrackingService       _svc;
    private readonly BudgetAuthorizationService _auth;

    public TimeEntriesController(ITimeEntryRepository repo, IJobRepository jobs,
        TimeTrackingService svc, BudgetAuthorizationService auth)
    { _repo = repo; _jobs = jobs; _svc = svc; _auth = auth; }

    [HttpGet]
    public async Task<IActionResult> GetAll(int budgetId,
        [FromQuery] int? jobId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var entries = await _repo.GetForBudgetAsync(budgetId, jobId, from, to);
        var dtos = entries.Select(e => TimeTrackingService.ToDto(e, e.Job?.HourlyRate ?? 0)).ToList();
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int budgetId, [FromBody] CreateTimeEntryDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        try
        {
            var entry = await _svc.CreateEntryAsync(budgetId, UserId, dto);
            var full  = await _repo.GetByIdAsync(entry.Id, budgetId);
            return Ok(TimeTrackingService.ToDto(full!, full!.Job.HourlyRate ?? 0));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { Message = ex.Message }); }
    }

    [HttpPut("{entryId:int}")]
    public async Task<IActionResult> Update(int budgetId, int entryId, [FromBody] CreateTimeEntryDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var entry = await _repo.GetByIdAsync(entryId, budgetId);
        if (entry == null) return NotFound();
        if (entry.LinkedTransactionId != null)
            return BadRequest(new { Message = "Postad tidpost kan inte redigeras utan att återkallas." });

        var duration = dto.DurationMinutes > 0 ? dto.DurationMinutes
            : (dto.StartTime.HasValue && dto.EndTime.HasValue
                ? (int)(dto.EndTime.Value - dto.StartTime.Value).TotalMinutes : entry.DurationMinutes);

        entry.JobId = dto.JobId; entry.Date = dto.Date;
        entry.StartTime = dto.StartTime; entry.EndTime = dto.EndTime;
        entry.DurationMinutes = Math.Max(0, duration);
        entry.Description = dto.Description; entry.IsBreak = dto.IsBreak;
        entry.HourlyRateOverride = dto.HourlyRateOverride;
        var updated = await _repo.UpdateAsync(entry);
        var full    = await _repo.GetByIdAsync(updated.Id, budgetId);
        return Ok(TimeTrackingService.ToDto(full!, full!.Job.HourlyRate ?? 0));
    }

    [HttpDelete("{entryId:int}")]
    public async Task<IActionResult> Delete(int budgetId, int entryId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var entry = await _repo.GetByIdAsync(entryId, budgetId);
        if (entry?.LinkedTransactionId != null)
            return BadRequest(new { Message = "Återkalla posten innan den tas bort." });
        await _repo.DeleteAsync(entryId, budgetId);
        return NoContent();
    }

    [HttpGet("period-summary")]
    public async Task<IActionResult> PeriodSummary(int budgetId, [FromQuery] int jobId, [FromQuery] string period)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var summary = await _svc.GetPeriodSummaryAsync(budgetId, jobId, period);
        return summary == null ? NotFound(new { Message = "Inga opublicerade poster för perioden." }) : Ok(summary);
    }

    [HttpPost("post-to-payroll")]
    public async Task<IActionResult> PostToPayroll(int budgetId, [FromBody] PostToPayrollDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        try
        {
            var tx = await _svc.PostToPayrollAsync(budgetId, UserId, dto);
            return Ok(new { TransactionId = tx.Id, tx.GrossAmount, tx.NetAmount, tx.Description });
        }
        catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
    }
}
