using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.Job;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Controllers;

[Authorize, Route("api/budgets/{budgetId:int}/jobs")]
public class JobsController : BaseApiController
{
    private readonly IJobRepository             _repo;
    private readonly ITimeEntryRepository       _entries;
    private readonly BudgetAuthorizationService _auth;

    public JobsController(IJobRepository repo, ITimeEntryRepository entries, BudgetAuthorizationService auth)
    { _repo = repo; _entries = entries; _auth = auth; }

    [HttpGet]
    public async Task<IActionResult> GetAll(int budgetId, [FromQuery] bool includeInactive = false)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var jobs = await _repo.GetForBudgetAsync(budgetId, includeInactive);
        var dtos = new List<JobDto>();
        foreach (var j in jobs)
        {
            var unposted    = await _entries.GetForBudgetAsync(budgetId, j.Id);
            var unpostedList = unposted.Where(e => e.LinkedTransactionId == null && !e.IsBreak).ToList();
            var rate         = j.HourlyRate ?? 0;
            dtos.Add(ToDto(j, unpostedList.Sum(e => e.DurationMinutes) / 60m,
                              unpostedList.Sum(e => e.GrossEarned(rate))));
        }
        return Ok(dtos);
    }

    [HttpGet("{jobId:int}")]
    public async Task<IActionResult> GetById(int budgetId, int jobId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var j = await _repo.GetByIdAsync(jobId, budgetId);
        return j == null ? NotFound() : Ok(ToDto(j, 0, 0));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int budgetId, [FromBody] CreateJobDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var job = new Job
        {
            BudgetId        = budgetId, UserId = UserId,
            Name            = dto.Name, EmployerName = dto.EmployerName,
            PayType         = dto.PayType, TransactionMode = dto.TransactionMode,
            GrossAmount     = dto.GrossAmount, HourlyRate = dto.HourlyRate,
            ProjectId       = dto.ProjectId, IsActive = dto.IsActive, Notes = dto.Notes
        };
        return Ok(ToDto(await _repo.CreateAsync(job), 0, 0));
    }

    [HttpPut("{jobId:int}")]
    public async Task<IActionResult> Update(int budgetId, int jobId, [FromBody] CreateJobDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var job = await _repo.GetByIdAsync(jobId, budgetId);
        if (job == null) return NotFound();
        job.Name = dto.Name; job.EmployerName = dto.EmployerName;
        job.PayType = dto.PayType; job.TransactionMode = dto.TransactionMode;
        job.GrossAmount = dto.GrossAmount; job.HourlyRate = dto.HourlyRate;
        job.ProjectId = dto.ProjectId; job.IsActive = dto.IsActive; job.Notes = dto.Notes;
        return Ok(ToDto(await _repo.UpdateAsync(job), 0, 0));
    }

    [HttpDelete("{jobId:int}")]
    public async Task<IActionResult> Delete(int budgetId, int jobId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        await _repo.DeleteAsync(jobId, budgetId);
        return NoContent();
    }

    private static JobDto ToDto(Job j, decimal unpostedHours, decimal unpostedGross) => new()
    {
        Id = j.Id, BudgetId = j.BudgetId, UserId = j.UserId, Name = j.Name,
        EmployerName = j.EmployerName, PayType = j.PayType, PayTypeLabel = SwedishPayType(j.PayType),
        TransactionMode = j.TransactionMode, GrossAmount = j.GrossAmount, HourlyRate = j.HourlyRate,
        ProjectId = j.ProjectId, ProjectName = j.Project?.Name, IsActive = j.IsActive,
        Notes = j.Notes, CreatedAt = j.CreatedAt,
        UnpostedHours = unpostedHours, UnpostedGross = unpostedGross
    };

    private static string SwedishPayType(JobPayType t) => t switch
    {
        JobPayType.Monthly       => "Månadsanställd",
        JobPayType.Yearly        => "Årsanställd",
        JobPayType.Hourly        => "Timbaserad",
        JobPayType.ProjectFixed  => "Projektbaserad",
        _ => t.ToString()
    };
}
