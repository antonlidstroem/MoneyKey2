using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.Common;
using MoneyKey.Core.DTOs.Journal;
using MoneyKey.Core.Services;
using MoneyKey.Domain.Enums;

namespace MoneyKey.API.Controllers;

[Authorize, Route("api/budgets/{budgetId:int}/journal")]
public class JournalController : BaseApiController
{
    private readonly JournalQueryService        _journal;
    private readonly BudgetAuthorizationService _auth;

    public JournalController(JournalQueryService journal, BudgetAuthorizationService auth)
    { _journal = journal; _auth = auth; }

    [HttpGet]
    public async Task<IActionResult> Get(int budgetId, [FromQuery] JournalQuery query)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        query.BudgetId = budgetId;

        var (items, total, summary) = await _journal.QueryAsync(query);

        return Ok(new
        {
            Result = new PagedResult<JournalEntryDto>
            {
                Items      = items,
                TotalCount = total,
                Page       = query.Page,
                PageSize   = query.PageSize
            },
            Summary = summary
        });
    }
}
