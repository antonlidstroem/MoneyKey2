using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoneyKey.API.Hubs;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.Common;
using MoneyKey.Core.DTOs.Kontering;
using MoneyKey.Core.DTOs.Transaction;
using MoneyKey.Core.Services;
using MoneyKey.DAL.Queries;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Controllers;

[Authorize, Route("api/budgets/{budgetId:int}/transactions")]
public class TransactionsController : BaseApiController
{
    private readonly ITransactionRepository   _repo;
    private readonly IKonteringRepository     _kontering;
    private readonly BudgetAuthorizationService _auth;
    private readonly ExportService            _export;
    private readonly IHubContext<BudgetHub>   _hub;
    private readonly SignalRFeatureService    _signalRFeature;

    public TransactionsController(
        ITransactionRepository repo,
        IKonteringRepository kontering,
        BudgetAuthorizationService auth,
        ExportService export,
        IHubContext<BudgetHub> hub,
        SignalRFeatureService signalRFeature)
    {
        _repo           = repo;
        _kontering      = kontering;
        _auth           = auth;
        _export         = export;
        _hub            = hub;
        _signalRFeature = signalRFeature;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int budgetId, [FromQuery] TransactionQuery query)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        query.BudgetId = budgetId;
        var (items, total) = await _repo.GetPagedAsync(query);
        var dtos = items.Select(MapDto).ToList();
        return Ok(new PagedResult<TransactionDto> { Items = dtos, TotalCount = total, Page = query.Page, PageSize = query.PageSize });
    }

    [HttpGet("{txId:int}")]
    public async Task<IActionResult> GetById(int budgetId, int txId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var t = await _repo.GetByIdAsync(txId, budgetId);
        return t == null ? NotFound() : Ok(MapDto(t));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int budgetId, [FromBody] CreateTransactionDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var tx = MapFromDto(dto, budgetId, UserId);
        tx = await _repo.CreateAsync(tx);
        if (dto.KonteringRows.Any())
            await _kontering.SaveRowsAsync(tx.Id, dto.KonteringRows.Select(MapKontering).ToList());
        await BroadcastAsync(_hub, _signalRFeature, budgetId, "TransactionCreated", tx.Id);
        return CreatedAtAction(nameof(GetById), new { budgetId, txId = tx.Id }, MapDto(tx));
    }

    [HttpPut("{txId:int}")]
    public async Task<IActionResult> Update(int budgetId, int txId, [FromBody] UpdateTransactionDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var tx = await _repo.GetByIdAsync(txId, budgetId);
        if (tx == null) return NotFound();
        ApplyUpdateDto(dto, tx, UserId);
        await _repo.UpdateAsync(tx);
        if (dto.KonteringRows.Any())
            await _kontering.SaveRowsAsync(tx.Id, dto.KonteringRows.Select(MapKontering).ToList());
        await BroadcastAsync(_hub, _signalRFeature, budgetId, "TransactionUpdated", tx.Id);
        return Ok(MapDto(tx));
    }

    [HttpDelete("{txId:int}")]
    public async Task<IActionResult> Delete(int budgetId, int txId)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        await _repo.DeleteAsync(txId, budgetId);
        await BroadcastAsync(_hub, _signalRFeature, budgetId, "TransactionDeleted", txId);
        return NoContent();
    }

    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete(int budgetId, [FromBody] BatchDeleteDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        await _repo.DeleteBatchAsync(dto.Ids, budgetId);
        await BroadcastAsync(_hub, _signalRFeature, budgetId, "TransactionsBatchDeleted");
        return Ok(new { Deleted = dto.Ids.Count });
    }

    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportPdf(int budgetId, [FromQuery] TransactionQuery q)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        q.BudgetId = budgetId;
        var txs = await _repo.GetForExportAsync(q);
        var pdf = _export.ExportToPdf(txs.Select(MapDto).ToList(), $"Budget {budgetId}");
        return File(pdf, "application/pdf", $"transaktioner_{DateTime.Today:yyyyMMdd}.pdf");
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel(int budgetId, [FromQuery] TransactionQuery q)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        q.BudgetId = budgetId;
        var txs  = await _repo.GetForExportAsync(q);
        var xlsx = _export.ExportToExcel(txs.Select(MapDto).ToList(), new List<Core.DTOs.Project.ProjectDto>(), $"Budget {budgetId}");
        return File(xlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"transaktioner_{DateTime.Today:yyyyMMdd}.xlsx");
    }

    private static TransactionDto MapDto(Transaction t) => new()
    {
        Id = t.Id, BudgetId = t.BudgetId, StartDate = t.StartDate, EndDate = t.EndDate,
        NetAmount = t.NetAmount, GrossAmount = t.GrossAmount, Description = t.Description,
        CategoryId = t.CategoryId, CategoryName = t.Category?.Name ?? "",
        Type = t.Type, Recurrence = t.Recurrence, IsActive = t.IsActive,
        Month = t.Month, Rate = t.Rate, ProjectId = t.ProjectId, ProjectName = t.Project?.Name,
        HasKontering = t.HasKontering, CreatedByUserId = t.CreatedByUserId, CreatedAt = t.CreatedAt,
        KonteringRows = t.KonteringRows.Select(k => new KonteringRowDto
        {
            Id = k.Id, KontoNr = k.KontoNr, CostCenter = k.CostCenter,
            Amount = k.Amount, Percentage = k.Percentage, Description = k.Description
        }).ToList()
    };

    private static Transaction MapFromDto(CreateTransactionDto dto, int budgetId, string userId) => new()
    {
        BudgetId = budgetId, StartDate = dto.StartDate, EndDate = dto.EndDate,
        NetAmount = AdjustSign(dto.NetAmount, dto.Type), GrossAmount = dto.GrossAmount,
        Description = dto.Description, CategoryId = dto.CategoryId, Type = dto.Type,
        Recurrence = dto.Recurrence, IsActive = dto.IsActive, Month = dto.Month,
        Rate = dto.Rate, ProjectId = dto.ProjectId, HasKontering = dto.KonteringRows.Any(),
        CreatedByUserId = userId
    };

    private static void ApplyUpdateDto(CreateTransactionDto dto, Transaction tx, string userId)
    {
        tx.StartDate = dto.StartDate; tx.EndDate = dto.EndDate;
        tx.NetAmount = AdjustSign(dto.NetAmount, dto.Type); tx.GrossAmount = dto.GrossAmount;
        tx.Description = dto.Description; tx.CategoryId = dto.CategoryId; tx.Type = dto.Type;
        tx.Recurrence = dto.Recurrence; tx.IsActive = dto.IsActive; tx.Month = dto.Month;
        tx.Rate = dto.Rate; tx.ProjectId = dto.ProjectId; tx.HasKontering = dto.KonteringRows.Any();
        tx.UpdatedByUserId = userId;
    }

    private static decimal AdjustSign(decimal v, TransactionType t) =>
        t == TransactionType.Expense ? -Math.Abs(v) : Math.Abs(v);

    private static KonteringRow MapKontering(KonteringRowDto d) => new()
    { KontoNr = d.KontoNr, CostCenter = d.CostCenter, Amount = d.Amount, Percentage = d.Percentage, Description = d.Description };
}
