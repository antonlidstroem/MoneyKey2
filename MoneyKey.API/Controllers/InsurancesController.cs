using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.API.Filters;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.Insurance;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Controllers;

[RequireFeature("Insurance")]
[Authorize, Route("api/budgets/{budgetId:int}/insurances")]
public class InsurancesController : BaseApiController
{
    private readonly IInsuranceRepository       _repo;
    private readonly BudgetAuthorizationService _auth;
    public InsurancesController(IInsuranceRepository repo, BudgetAuthorizationService auth)
    { _repo = repo; _auth = auth; }

    [HttpGet]
    public async Task<IActionResult> GetAll(int budgetId, [FromQuery] bool includeInactive = false)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Viewer)) return Forbid();
        return Ok((await _repo.GetForBudgetAsync(budgetId, includeInactive)).Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int budgetId, [FromBody] CreateInsuranceDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var ins = new Insurance { BudgetId = budgetId, UserId = UserId, InsuranceType = dto.InsuranceType,
            Name = dto.Name, Provider = dto.Provider, PremiumAmount = dto.PremiumAmount, PayPeriod = dto.PayPeriod,
            StartDate = dto.StartDate, RenewalDate = dto.RenewalDate, PolicyNumber = dto.PolicyNumber, Notes = dto.Notes };
        return Ok(ToDto(await _repo.CreateAsync(ins)));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int budgetId, int id, [FromBody] CreateInsuranceDto dto)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        var ins = await _repo.GetByIdAsync(id, budgetId);
        if (ins == null) return NotFound();
        ins.InsuranceType = dto.InsuranceType; ins.Name = dto.Name; ins.Provider = dto.Provider;
        ins.PremiumAmount = dto.PremiumAmount; ins.PayPeriod = dto.PayPeriod;
        ins.StartDate = dto.StartDate; ins.RenewalDate = dto.RenewalDate;
        ins.PolicyNumber = dto.PolicyNumber; ins.Notes = dto.Notes;
        return Ok(ToDto(await _repo.UpdateAsync(ins)));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int budgetId, int id)
    {
        if (!await _auth.HasRoleAsync(budgetId, UserId, BudgetMemberRole.Editor)) return Forbid();
        await _repo.DeleteAsync(id, budgetId); return NoContent();
    }

    private static InsuranceDto ToDto(Insurance i) => new()
    {
        Id = i.Id, InsuranceType = i.InsuranceType, TypeLabel = SwType(i.InsuranceType),
        Name = i.Name, Provider = i.Provider, PremiumAmount = i.PremiumAmount,
        PayPeriod = i.PayPeriod, PayPeriodLabel = SwPayPeriod(i.PayPeriod), MonthlyCost = i.MonthlyCost,
        StartDate = i.StartDate, RenewalDate = i.RenewalDate, PolicyNumber = i.PolicyNumber,
        IsActive = i.IsActive, Notes = i.Notes
    };
    private static string SwType(InsuranceType t) => t switch
    {
        InsuranceType.Home => "Hemförsäkring", InsuranceType.Car => "Bilförsäkring",
        InsuranceType.Life => "Livförsäkring", InsuranceType.Accident => "Olycksfallsförsäkring",
        InsuranceType.IncomeLoss => "Inkomstförsäkring", InsuranceType.Pension => "Pensionsförsäkring",
        InsuranceType.Health => "Sjukvårdsförsäkring", _ => "Övrigt"
    };
    private static string SwPayPeriod(InsurancePayPeriod p) => p switch
    { InsurancePayPeriod.Monthly => "Månadsvis", InsurancePayPeriod.Quarterly => "Kvartalsvis", _ => "Årsvis" };
}
