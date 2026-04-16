using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.Audit;
using MoneyKey.Core.DTOs.Common;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;

namespace MoneyKey.API.Controllers;

[Authorize, Route("api/budgets/{budgetId:int}/audit")]
public class AuditController : BaseApiController
{
    private readonly IAuditRepository           _repo;
    private readonly BudgetAuthorizationService _auth;

    public AuditController(IAuditRepository repo, BudgetAuthorizationService auth)
    { _repo = repo; _auth = auth; }

    [HttpGet]
    public async Task<IActionResult> GetAuditLog(int budgetId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Auditor)) return Forbid();
        var (items, total) = await _repo.GetPagedAsync(budgetId, page, pageSize);
        return Ok(new PagedResult<AuditLogDto>
        {
            Items = items.Select(a => new AuditLogDto
            {
                Id = a.Id, UserEmail = a.UserEmail, EntityName = a.EntityName,
                EntityId = a.EntityId, Action = a.Action, OldValues = a.OldValues,
                NewValues = a.NewValues, Timestamp = a.Timestamp
            }).ToList(),
            TotalCount = total, Page = page, PageSize = pageSize
        });
    }
}
