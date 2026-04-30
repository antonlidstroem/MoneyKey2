using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.API.Filters;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.Loan;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Controllers;

[RequireFeature("Loans")]
[Authorize, Route("api/budgets/{budgetId:int}/loans")]
public class LoansController : BaseApiController
{
    private readonly ILoanRepository            _repo;
    private readonly BudgetAuthorizationService _auth;
    public LoansController(ILoanRepository repo, BudgetAuthorizationService auth)
    { _repo = repo; _auth = auth; }

    [HttpGet]
    public async Task<IActionResult> GetAll(int budgetId, [FromQuery] bool includeInactive = false)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        var loans = await _repo.GetForBudgetAsync(budgetId, includeInactive);
        return Ok(loans.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int budgetId, [FromBody] CreateLoanDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var loan = new Loan { BudgetId = budgetId, UserId = UserId, LoanType = dto.LoanType, Name = dto.Name,
            LenderName = dto.LenderName, OriginalAmount = dto.OriginalAmount, CurrentBalance = dto.CurrentBalance,
            InterestRate = dto.InterestRate, MonthlyPayment = dto.MonthlyPayment, PayoffDate = dto.PayoffDate,
            StartDate = dto.StartDate, Notes = dto.Notes };
        return Ok(ToDto(await _repo.CreateAsync(loan)));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int budgetId, int id, [FromBody] CreateLoanDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var loan = await _repo.GetByIdAsync(id, budgetId);
        if (loan == null) return NotFound();
        loan.LoanType = dto.LoanType; loan.Name = dto.Name; loan.LenderName = dto.LenderName;
        loan.OriginalAmount = dto.OriginalAmount; loan.CurrentBalance = dto.CurrentBalance;
        loan.InterestRate = dto.InterestRate; loan.MonthlyPayment = dto.MonthlyPayment;
        loan.PayoffDate = dto.PayoffDate; loan.StartDate = dto.StartDate; loan.Notes = dto.Notes;
        return Ok(ToDto(await _repo.UpdateAsync(loan)));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int budgetId, int id)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        await _repo.DeleteAsync(id, budgetId); return NoContent();
    }

    private static LoanDto ToDto(Loan l)
    {
        var r      = l.EffectiveMonthlyRate;
        var months = (l.MonthlyPayment > 0 && r > 0)
            ? (int)(-Math.Log(1 - (double)(l.CurrentBalance * r / l.MonthlyPayment)) / Math.Log(1 + (double)r))
            : 0;
        return new LoanDto { Id = l.Id, LoanType = l.LoanType,
            TypeLabel = SwLoanType(l.LoanType), Name = l.Name, LenderName = l.LenderName,
            OriginalAmount = l.OriginalAmount, CurrentBalance = l.CurrentBalance,
            InterestRate = l.InterestRate, MonthlyPayment = l.MonthlyPayment,
            PayoffDate = l.PayoffDate, StartDate = l.StartDate, IsActive = l.IsActive,
            Notes = l.Notes, TotalInterestEstimate = l.TotalInterestEstimate, EstimatedMonthsLeft = months };
    }
    private static string SwLoanType(LoanType t) => t switch
    {
        LoanType.Mortgage => "Bolån", LoanType.CarLoan => "Billån",
        LoanType.StudentLoan => "Studielån", LoanType.PersonalLoan => "Privatlån",
        LoanType.CreditCard => "Kreditkort", _ => "Övrigt"
    };
}
