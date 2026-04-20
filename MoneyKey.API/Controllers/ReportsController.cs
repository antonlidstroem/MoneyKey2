using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.Summary;
using MoneyKey.DAL.Queries;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;

namespace MoneyKey.API.Controllers;

[Authorize, Route("api/budgets/{budgetId:int}/reports")]
public class ReportsController : BaseApiController
{
    private readonly ITransactionRepository     _txRepo;
    private readonly BudgetAuthorizationService _auth;
    private readonly ICategoryAccountMappingRepository _catMapRepo;
    private readonly IJournalRepository _journal;


    public ReportsController(ITransactionRepository txRepo, BudgetAuthorizationService auth,
        ICategoryAccountMappingRepository catMapRepo, IJournalRepository journal)
    { _txRepo = txRepo; _auth = auth; _catMapRepo = catMapRepo; _journal = journal; }

    [HttpGet("monthly-summary")]
    public async Task<IActionResult> MonthlySummary(int budgetId, [FromQuery] int year = 0)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        if (year == 0) year = DateTime.Today.Year;
        var q = new TransactionQuery
        {
            BudgetId          = budgetId,
            FilterByStartDate = true, StartDate = new DateTime(year, 1, 1),
            FilterByEndDate   = true, EndDate   = new DateTime(year, 12, 31),
            PageSize          = int.MaxValue,
            // Exclude milersättning/vab auto-transactions to avoid double-counting
            ExcludeLinked     = true
        };
        var (txs, _) = await _txRepo.GetPagedAsync(q);
        var rows = txs
            .GroupBy(t => t.StartDate.Month)
            .Select(g => new MonthlyRow
            {
                Year     = year,
                Month    = g.Key,
                Income   = g.Where(t => t.NetAmount > 0).Sum(t => t.NetAmount),
                Expenses = g.Where(t => t.NetAmount < 0).Sum(t => t.NetAmount)
            })
            .OrderBy(r => r.Month)
            .ToList();

        // Fill in months with no data so charts always show all 12 bars
        var allMonths = Enumerable.Range(1, 12)
            .Select(m => rows.FirstOrDefault(r => r.Month == m) ?? new MonthlyRow { Year = year, Month = m })
            .ToList();

        return Ok(new MonthlySummary { Rows = allMonths });
    }

    [HttpGet("category-breakdown")]
    public async Task<IActionResult> CategoryBreakdown(int budgetId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var q = new TransactionQuery
        {
            BudgetId          = budgetId,
            FilterByStartDate = from.HasValue, StartDate = from,
            FilterByEndDate   = to.HasValue,   EndDate   = to,
            PageSize          = int.MaxValue,
            ExcludeLinked     = true
        };
        var (txs, _) = await _txRepo.GetPagedAsync(q);
        var breakdown = txs
            .Where(t => t.NetAmount < 0)
            .GroupBy(t => t.Category?.Name ?? "Okänd")
            .Select(g => new CategoryBreakdownItem { Category = g.Key, Total = Math.Abs(g.Sum(t => t.NetAmount)) })
            .OrderByDescending(x => x.Total)
            .Take(10);
        return Ok(breakdown);
    }
    [HttpGet("{budgetId:int}/reports/export-sie")]
    public async Task<IActionResult> ExportSie(int budgetId,
        [FromQuery] int year = 0,
        [FromQuery] string companyName = "Budget",
        [FromQuery] string? orgNumber = null)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Owner)) return Forbid();
        year = year == 0 ? DateTime.Today.Year : year;
        var q = new TransactionQuery { BudgetId = budgetId, StartDate = new DateTime(year,1,1), EndDate = new DateTime(year,12,31), ExcludeLinked = true };
        var txs = await _txRepo.GetPagedAsync(q);
        var txDtos = txs.Items.Select(_journal.MapToDto).ToList();
        var mappings = await _catMapRepo.GetForBudgetAsync(budgetId);
        var req = new MoneyKey.Core.DTOs.Sie.SieExportRequestDto { BudgetId = budgetId, Year = year, CompanyName = companyName, OrgNumber = orgNumber };
        var bytes = MoneyKey.Core.Services.SieExportService.Generate(req, txDtos, mappings);
        return File(bytes, "application/octet-stream", $"sie4_{companyName}_{year}.se");
    }

}
