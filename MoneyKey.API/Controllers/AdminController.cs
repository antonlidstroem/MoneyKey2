using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKey.API.Services;

namespace MoneyKey.API.Controllers;

/// <summary>
/// Superadmin endpoints. Only accessible to the configured AdminSetup:Email user.
/// </summary>
[Authorize, Route("api/admin")]
public class AdminController : BaseApiController
{
    private readonly SignalRFeatureService _signalR;
    private readonly IConfiguration       _cfg;

    public AdminController(SignalRFeatureService signalR, IConfiguration cfg)
    {
        _signalR = signalR;
        _cfg     = cfg;
    }

    [HttpGet("signalr-status")]
    public async Task<IActionResult> GetSignalRStatus()
    {
        var enabled = await _signalR.IsEnabledAsync();
        return Ok(new { Enabled = enabled });
    }

    [HttpPatch("signalr-toggle")]
    public async Task<IActionResult> ToggleSignalR([FromBody] SetSignalREnabledDto dto)
    {
        if (!IsSuperAdmin()) return Forbid();
        await _signalR.SetEnabledAsync(dto.Enabled);
        return Ok(new { Enabled = dto.Enabled, Message = dto.Enabled ? "SignalR aktiverat." : "SignalR inaktiverat." });
    }

    private bool IsSuperAdmin()
    {
        var adminEmail = _cfg["AdminSetup:Email"];
        return !string.IsNullOrEmpty(adminEmail) &&
               string.Equals(UserEmail, adminEmail, StringComparison.OrdinalIgnoreCase);
    }
}

