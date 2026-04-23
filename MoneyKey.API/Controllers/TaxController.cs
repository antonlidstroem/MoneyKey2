using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.Core.DTOs.Tax;
using MoneyKey.Core.Services;

namespace MoneyKey.API.Controllers;

[Authorize, Route("api/tax")]
public class TaxController : BaseApiController
{
    [HttpPost("calculate")]
    public IActionResult Calculate([FromBody] TaxCalculationRequestDto dto)
    {
        if (dto.GrossIncome < 0) return BadRequest(new { Message = "Inkomst kan inte vara negativ." });
        if (dto.MunicipalTaxRate is < 10 or > 40) return BadRequest(new { Message = "Kommunalskatt bör vara mellan 10 och 40 procent." });
        return Ok(TaxCalculatorService.Calculate(dto));
    }
}
