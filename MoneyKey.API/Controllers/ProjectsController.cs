using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoneyKey.API.Hubs;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.Project;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Controllers;

[Authorize, Route("api/budgets/{budgetId:int}/projects")]
public class ProjectsController : BaseApiController
{
    private readonly IProjectRepository _repo;
    private readonly BudgetAuthorizationService _auth;
    private readonly IHubContext<BudgetHub> _hub;
    private readonly SignalRFeatureService _signalRFeature;

    public ProjectsController(IProjectRepository repo, BudgetAuthorizationService auth,
        IHubContext<BudgetHub> hub, SignalRFeatureService signalRFeature)
    { _repo = repo; _auth = auth; _hub = hub; _signalRFeature = signalRFeature; }

    [HttpGet]
    public async Task<IActionResult> GetAll(int budgetId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var withSpent = await _repo.GetForBudgetWithSpentAsync(budgetId);
        return Ok(withSpent.Select(x => Map(x.Project, x.SpentAmount)));
    }

    [HttpGet("{projectId:int}")]
    public async Task<IActionResult> GetById(int budgetId, int projectId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var all = await _repo.GetForBudgetWithSpentAsync(budgetId);
        var found = all.FirstOrDefault(x => x.Project.Id == projectId);
        if (found.Project == null) return NotFound();
        return Ok(Map(found.Project, found.SpentAmount));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int budgetId, [FromBody] CreateProjectDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var p = await _repo.CreateAsync(new Project
        {
            BudgetId = budgetId,
            Name = dto.Name,
            Description = dto.Description,
            BudgetAmount = dto.BudgetAmount,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        });
        await BroadcastAsync(_hub, _signalRFeature, budgetId, "ProjectCreated", p.Id);
        return CreatedAtAction(nameof(GetById), new { budgetId, projectId = p.Id }, Map(p, 0));
    }

    [HttpPut("{projectId:int}")]
    public async Task<IActionResult> Update(int budgetId, int projectId, [FromBody] UpdateProjectDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var p = await _repo.GetByIdAsync(projectId, budgetId);
        if (p == null) return NotFound();

        p.Name = dto.Name; p.Description = dto.Description; p.BudgetAmount = dto.BudgetAmount;
        p.StartDate = dto.StartDate; p.EndDate = dto.EndDate; p.IsActive = dto.IsActive;
        await _repo.UpdateAsync(p);
        await BroadcastAsync(_hub, _signalRFeature, budgetId, "ProjectUpdated", p.Id);

        // Return updated DTO so the frontend can update its local state correctly.
        var all = await _repo.GetForBudgetWithSpentAsync(budgetId);
        var found = all.FirstOrDefault(x => x.Project.Id == projectId);
        return Ok(Map(found.Project ?? p, found.SpentAmount));
    }

    [HttpDelete("{projectId:int}")]
    public async Task<IActionResult> Delete(int budgetId, int projectId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        await _repo.DeleteAsync(projectId, budgetId);
        await BroadcastAsync(_hub, _signalRFeature, budgetId, "ProjectDeleted", projectId);
        return NoContent();
    }

    private static ProjectDto Map(Project p, decimal spent) => new()
    {
        Id = p.Id,
        BudgetId = p.BudgetId,
        Name = p.Name,
        Description = p.Description,
        BudgetAmount = p.BudgetAmount,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        IsActive = p.IsActive,
        SpentAmount = spent
    };
}