using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoneyKey.API.Hubs;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.Import;
using MoneyKey.Core.Services;
using MoneyKey.Domain.Enums;

namespace MoneyKey.API.Controllers;

[Authorize, Route("api/budgets/{budgetId:int}/import")]
public class ImportController : BaseApiController
{
    private readonly ImportService _svc;
    private readonly BudgetAuthorizationService _auth;
    private readonly IHubContext<BudgetHub> _hub;
    private readonly SignalRFeatureService _signalRFeature;

    public ImportController(ImportService svc, BudgetAuthorizationService auth,
        IHubContext<BudgetHub> hub, SignalRFeatureService signalRFeature)
    { _svc = svc; _auth = auth; _hub = hub; _signalRFeature = signalRFeature; }

    /// <summary>
    /// Parses an uploaded CSV file and returns a preview.
    /// Optional query params: dateColIndex, amountColIndex, descColIndex
    /// allow the client to specify column positions when auto-detection fails.
    /// </summary>
    [HttpPost("preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Preview(
        int budgetId,
        IFormFile file,
        [FromQuery] int? dateColIndex = null,
        [FromQuery] int? amountColIndex = null,
        [FromQuery] int? descColIndex = null)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        if (file == null || file.Length == 0) return BadRequest("Ingen fil uppladdad.");

        await using var stream = file.OpenReadStream();

        ImportSessionDto session;
        if (dateColIndex.HasValue && amountColIndex.HasValue)
        {
            // Manual column mapping requested
            session = await _svc.PreviewWithMappingAsync(
                stream, budgetId, UserId,
                dateColIndex.Value, amountColIndex.Value, descColIndex ?? -1);
        }
        else
        {
            // Auto-detect
            session = await _svc.PreviewAsync(stream, budgetId, UserId);
        }

        return Ok(session);
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm(int budgetId, [FromBody] ConfirmImportDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var count = await _svc.ConfirmAsync(dto, budgetId, UserId);
        await BroadcastAsync(_hub, _signalRFeature, budgetId, "TransactionsImported");
        return Ok(new { Imported = count });
    }
}