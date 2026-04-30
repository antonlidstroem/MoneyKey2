using MoneyKey.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MoneyKey.API.Services;
using MoneyKey.Core.DTOs.Auth;
using MoneyKey.Core.DTOs.Budget;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Models;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Controllers;

[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IUserSubscriptionRepository _subRepo;
    private readonly TokenService _tokens;
    private readonly BudgetDbContext _db;
    private const string CookieName = "mk_refresh";

    public AuthController(
        UserManager<ApplicationUser> users,
        IUserSubscriptionRepository subRepo,
        TokenService tokens,
        BudgetDbContext db)
    {
        _users = users;
        _subRepo = subRepo;
        _tokens = tokens;
        _db = db;
    }

    [HttpPost("register")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DisplayName = dto.DisplayName.Trim()
        };
        var result = await _users.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

        await _subRepo.UpsertAsync(new UserSubscription { UserId = user.Id });

        var budget = new Budget { Name = $"{dto.FirstName}s budget", OwnerId = user.Id };
        _db.Budgets.Add(budget);
        var membership = new BudgetMembership
        {
            BudgetId = 0,
            UserId = user.Id,
            Role = BudgetMemberRole.Owner,
            AcceptedAt = DateTime.UtcNow
        };
        _db.BudgetMemberships.Add(membership);
        membership.Budget = budget;
        await _db.SaveChangesAsync();

        return await IssueTokensAsync(user);
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _users.FindByEmailAsync(dto.Email);
        if (user == null || !await _users.CheckPasswordAsync(user, dto.Password))
            return Unauthorized(new { Message = "Ogiltiga inloggningsuppgifter." });
        return await IssueTokensAsync(user);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var raw = Request.Cookies[CookieName];
        if (string.IsNullOrEmpty(raw))
            return Unauthorized(new { Message = "Refresh token saknas." });

        var token = await _tokens.ValidateAndRevokeRefreshTokenAsync(raw);
        if (token == null)
            return Unauthorized(new { Message = "Ogiltig eller utgången session." });

        var user = await _users.FindByIdAsync(token.UserId);
        if (user == null) return Unauthorized();
        return await IssueTokensAsync(user);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _tokens.RevokeAllRefreshTokensAsync(UserId);
        // SameSite=None required for cross-origin cookie deletion (Azure SWA + App Service)
        Response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
        });
        return Ok(new { Message = "Utloggad." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await _users.FindByIdAsync(UserId);
        if (user == null) return NotFound();
        return Ok(new UserDto(user.Id, user.Email!, user.FirstName, user.LastName,
            await GetMembershipsAsync(user.Id)));
    }

    [HttpPost("accept-invite")]
    [Authorize]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Token))
            return BadRequest(new { Message = "Token saknas." });

        var membership = await _db.BudgetMemberships
            .Include(m => m.Budget)
            .FirstOrDefaultAsync(m => m.InviteToken == dto.Token && m.AcceptedAt == null);

        if (membership == null)
            return BadRequest(new { Message = "Ogiltig eller redan använd inbjudningslänk." });

        var alreadyMember = await _db.BudgetMemberships
            .AnyAsync(m => m.BudgetId == membership.BudgetId
                        && m.UserId == UserId
                        && m.Id != membership.Id);
        if (alreadyMember)
            return BadRequest(new { Message = "Du är redan medlem i denna budget." });

        membership.UserId = UserId;
        membership.AcceptedAt = DateTime.UtcNow;
        membership.InviteToken = null;
        await _db.SaveChangesAsync();

        var user = await _users.FindByIdAsync(UserId);
        if (user == null) return Unauthorized();
        var memberships = await GetMembershipsAsync(UserId);
        var access = _tokens.GenerateAccessToken(user, memberships);

        return Ok(new AuthResultDto(access,
            new UserDto(user.Id, user.Email!, user.FirstName, user.LastName, memberships)));
    }

    [Authorize]
    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount(
        [FromServices] MoneyKey.Core.Services.AccountDeletionService svc)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        try
        {
            await svc.DeleteAccountAsync(userId);
            return Ok(new { Message = "Ditt konto och all persondata har raderats." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = $"Radering misslyckades: {ex.Message}" });
        }
    }

    private async Task<IActionResult> IssueTokensAsync(ApplicationUser user)
    {
        var memberships = await GetMembershipsAsync(user.Id);
        var access = _tokens.GenerateAccessToken(user, memberships);
        var (raw, _) = await _tokens.GenerateRefreshTokenAsync(user.Id);

        // SameSite=None is required for cross-origin cookie usage (Azure Static Web Apps
        // served from a different domain than the API on Azure App Service).
        // Secure=true is mandatory when SameSite=None.
        Response.Cookies.Append(CookieName, raw, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });

        return Ok(new AuthResultDto(access,
            new UserDto(user.Id, user.Email!, user.FirstName, user.LastName, memberships)));
    }

    [HttpPut("display-name")]
    [Authorize]
    public async Task<IActionResult> UpdateDisplayName([FromBody] UpdateDisplayNameDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DisplayName))
            return BadRequest(new { Message = "Visningsnamnet får inte vara tomt." });

        var user = await _users.FindByIdAsync(UserId);
        if (user == null) return NotFound();
        user.DisplayName = dto.DisplayName.Trim();
        await _users.UpdateAsync(user);
        return Ok(new { Message = "Visningsnamn uppdaterat." });
    }

    // Minimal record (kan läggas i MoneyKey.Core/DTOs/Auth/):
    public record UpdateDisplayNameDto(string DisplayName);

    private async Task<List<BudgetMembershipDto>> GetMembershipsAsync(string userId) =>
        await _db.BudgetMemberships
            .Where(m => m.UserId == userId && m.AcceptedAt != null)
            .Include(m => m.Budget)
            .Select(m => new BudgetMembershipDto(m.BudgetId, m.Budget.Name, m.Role))
            .ToListAsync();
}