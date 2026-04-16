using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoneyKey.API.Hubs;
using MoneyKey.API.Services;
using MoneyKey.API.Services.Email;
using MoneyKey.Core.DTOs.Common;
using MoneyKey.Core.DTOs.Receipt;
using MoneyKey.Core.Services;
using MoneyKey.DAL.Queries;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;

namespace MoneyKey.API.Controllers;

[Authorize, Route("api/budgets/{budgetId:int}/receipts")]
public class ReceiptsController : BaseApiController
{
    private readonly IReceiptRepository         _repo;
    private readonly ReceiptService             _svc;
    private readonly BudgetAuthorizationService _auth;
    private readonly IHubContext<BudgetHub>     _hub;
    private readonly IEmailService              _email;
    private readonly IConfiguration             _cfg;
    private readonly SignalRFeatureService      _signalRFeature;

    public ReceiptsController(IReceiptRepository repo, ReceiptService svc,
        BudgetAuthorizationService auth, IHubContext<BudgetHub> hub,
        IEmailService email, IConfiguration cfg, SignalRFeatureService signalRFeature)
    {
        _repo           = repo;
        _svc            = svc;
        _auth           = auth;
        _hub            = hub;
        _email          = email;
        _cfg            = cfg;
        _signalRFeature = signalRFeature;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int budgetId, [FromQuery] ReceiptQuery query)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        query.BudgetId = budgetId;
        var (batches, total) = await _repo.GetPagedAsync(query);
        return Ok(new PagedResult<ReceiptBatchDto>
        {
            Items      = batches.Select(Map).ToList(),
            TotalCount = total,
            Page       = query.Page,
            PageSize   = query.PageSize
        });
    }

    [HttpGet("{batchId:int}")]
    public async Task<IActionResult> GetById(int budgetId, int batchId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var b = await _repo.GetByIdAsync(batchId, budgetId);
        return b == null ? NotFound() : Ok(Map(b));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int budgetId, [FromBody] CreateReceiptBatchDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var batch = await _svc.CreateBatchAsync(budgetId, UserId, UserEmail, dto);
        await BroadcastAsync(_hub, _signalRFeature, budgetId, "ReceiptBatchCreated", batch.Id);
        return CreatedAtAction(nameof(GetById), new { budgetId, batchId = batch.Id }, Map(batch));
    }

    [HttpPut("{batchId:int}")]
    public async Task<IActionResult> Update(int budgetId, int batchId, [FromBody] UpdateReceiptBatchDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var b = await _repo.GetByIdAsync(batchId, budgetId);
        if (b == null) return NotFound();
        if (b.Status != ReceiptBatchStatus.Draft) return BadRequest("Kan bara redigera utkast.");
        b.Label = dto.Label; b.BatchCategoryId = dto.BatchCategoryId; b.ProjectId = dto.ProjectId;
        await _repo.UpdateAsync(b);
        return Ok(Map(b));
    }

    [HttpDelete("{batchId:int}")]
    public async Task<IActionResult> Delete(int budgetId, int batchId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var b = await _repo.GetByIdAsync(batchId, budgetId);
        if (b == null) return NotFound();
        if (b.Status is not (ReceiptBatchStatus.Draft or ReceiptBatchStatus.Rejected))
            return BadRequest("Kan bara ta bort utkast eller avslagna underlag.");
        await _repo.DeleteAsync(batchId, budgetId);
        await BroadcastAsync(_hub, _signalRFeature, budgetId, "ReceiptBatchDeleted", batchId);
        return NoContent();
    }

    [HttpPost("{batchId:int}/lines")]
    public async Task<IActionResult> AddLine(int budgetId, int batchId, [FromBody] CreateReceiptLineDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var line = await _svc.AddLineAsync(batchId, budgetId, dto with { BudgetId = budgetId });
        await BroadcastAsync(_hub, _signalRFeature, budgetId, "ReceiptLineAdded", batchId);
        return Ok(MapLine(line));
    }

    [HttpPut("{batchId:int}/lines/{lineId:int}")]
    public async Task<IActionResult> UpdateLine(int budgetId, int batchId, int lineId, [FromBody] UpdateReceiptLineDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var b = await _repo.GetByIdAsync(batchId, budgetId);
        if (b?.Status != ReceiptBatchStatus.Draft) return BadRequest("Kan bara redigera rader i utkast.");
        var line = await _repo.GetLineAsync(lineId, batchId);
        if (line == null) return NotFound();
        line.Date = dto.Date; line.Amount = dto.Amount; line.Vendor = dto.Vendor; line.Description = dto.Description;
        await _repo.UpdateLineAsync(line);
        return Ok(MapLine(line));
    }

    [HttpDelete("{batchId:int}/lines/{lineId:int}")]
    public async Task<IActionResult> DeleteLine(int budgetId, int batchId, int lineId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var b = await _repo.GetByIdAsync(batchId, budgetId);
        if (b?.Status != ReceiptBatchStatus.Draft) return BadRequest("Kan bara ta bort rader i utkast.");
        await _repo.DeleteLineAsync(lineId, batchId);
        return NoContent();
    }

    [HttpPatch("{batchId:int}/status")]
    public async Task<IActionResult> UpdateStatus(int budgetId, int batchId, [FromBody] UpdateReceiptStatusDto dto)
    {
        var membership = await _auth.GetMembershipAsync(budgetId, UserId);
        if (membership == null) return Forbid();
        try
        {
            var updated = await _svc.UpdateStatusAsync(batchId, budgetId, dto.NewStatus, UserId, membership.Role, dto.RejectionReason);
            if (dto.NewStatus == ReceiptBatchStatus.Submitted)
                await BroadcastAsync(_hub, _signalRFeature, budgetId, "ReceiptBatchSubmitted", batchId);
            else
            {
                var batch = await _repo.GetByIdAsync(batchId, budgetId);
                if (batch?.CreatedByEmail != null)
                    await _email.SendReceiptStatusChangedAsync(batch.CreatedByEmail, batch.Label, dto.NewStatus.ToString(), dto.RejectionReason);
                await BroadcastAsync(_hub, _signalRFeature, budgetId, "ReceiptBatchStatusChanged", batchId);
            }
            return Ok(Map(updated));
        }
        catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
    }

    [HttpGet("{batchId:int}/export/pdf")]
    public async Task<IActionResult> ExportPdf(int budgetId, int batchId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var b = await _repo.GetByIdAsync(batchId, budgetId);
        if (b == null) return NotFound();
        var pdf = _svc.ExportBatchToPdf(b, $"Budget {budgetId}");
        return File(pdf, "application/pdf", $"utlagg_{b.Label.Replace(" ", "_")}_{DateTime.Today:yyyyMMdd}.pdf");
    }

    [HttpGet("/api/budgets/{budgetId:int}/receipt-categories")]
    public async Task<IActionResult> GetCategories(int budgetId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var cats = await _svc.GetCategoriesAsync();
        return Ok(cats.Select(c => new ReceiptBatchCategoryDto { Id = c.Id, Name = c.Name, IconName = c.IconName, Description = c.Description }));
    }

    private static ReceiptBatchDto Map(Domain.Models.ReceiptBatch b) => new()
    {
        Id = b.Id, BudgetId = b.BudgetId, ProjectId = b.ProjectId, ProjectName = b.Project?.Name,
        Label = b.Label, BatchCategoryId = b.BatchCategoryId,
        BatchCategoryName = b.Category?.Name ?? "", BatchCategoryIcon = b.Category?.IconName,
        Status = b.Status, StatusLabel = SwedishStatus(b.Status),
        CreatedByUserId = b.CreatedByUserId, CreatedByEmail = b.CreatedByEmail,
        SubmittedAt = b.SubmittedAt, ApprovedAt = b.ApprovedAt,
        RejectedAt = b.RejectedAt, RejectionReason = b.RejectionReason, ReimbursedAt = b.ReimbursedAt,
        CreatedAt = b.CreatedAt, TotalAmount = b.Lines.Sum(l => l.Amount), LineCount = b.Lines.Count,
        Lines = b.Lines.Select(MapLine).ToList()
    };

    private static ReceiptLineDto MapLine(Domain.Models.ReceiptLine l) => new()
    {
        Id = l.Id, BatchId = l.BatchId, SequenceNumber = l.SequenceNumber, ReferenceCode = l.ReferenceCode,
        Date = l.Date, Amount = l.Amount, Currency = l.Currency, Vendor = l.Vendor,
        Description = l.Description, LinkedTransactionId = l.LinkedTransactionId, DigitalReceiptUrl = l.DigitalReceiptUrl
    };

    private static string SwedishStatus(ReceiptBatchStatus s) => s switch
    {
        ReceiptBatchStatus.Draft      => "Utkast",
        ReceiptBatchStatus.Submitted  => "Inskickad",
        ReceiptBatchStatus.Approved   => "Godkänd",
        ReceiptBatchStatus.Rejected   => "Avslagen",
        ReceiptBatchStatus.Reimbursed => "Utbetald",
        _                             => s.ToString()
    };
}
