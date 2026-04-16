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
    private readonly TokenService                 _tokens;
    private readonly BudgetDbContext              _db;
    private const string CookieName = "mk_refresh";

    public AuthController(UserManager<ApplicationUser> users, TokenService tokens, BudgetDbContext db)
    {
        _users  = users;
        _tokens = tokens;
        _db     = db;
    }

    [HttpPost("register")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var user   = new ApplicationUser { UserName = dto.Email, Email = dto.Email, FirstName = dto.FirstName, LastName = dto.LastName };
        var result = await _users.CreateAsync(user, dto.Password);
        if (!result.Succeeded) return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

        // FIX MEDIUM-4: Create Budget AND BudgetMembership in a single SaveChanges so
        // no orphaned budget can exist without its owner membership if the second write fails.
        var budget = new Budget { Name = $"{dto.FirstName}s budget", OwnerId = user.Id };
        _db.Budgets.Add(budget);
        // EF will resolve budget.Id after the add because BudgetMembership is added to
        // the same context — both are saved atomically in one round-trip.
        var membership = new BudgetMembership { BudgetId = 0, UserId = user.Id, Role = BudgetMemberRole.Owner, AcceptedAt = DateTime.UtcNow };
        _db.BudgetMemberships.Add(membership);
        // Link via navigation so EF assigns budget.Id before inserting membership
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
        if (string.IsNullOrEmpty(raw)) return Unauthorized(new { Message = "Refresh token saknas." });

        // FIX SECURITY-1: Validate and revoke in a single atomic DB operation to prevent
        // the token-replay race condition where two concurrent requests both pass
        // ValidateRefreshTokenAsync before either has called RevokeRefreshTokenAsync.
        var token = await _tokens.ValidateAndRevokeRefreshTokenAsync(raw);
        if (token == null) return Unauthorized(new { Message = "Ogiltig eller utgången session." });

        var user = await _users.FindByIdAsync(token.UserId);
        if (user == null) return Unauthorized();
        return await IssueTokensAsync(user);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _tokens.RevokeAllRefreshTokensAsync(UserId);
        Response.Cookies.Delete(CookieName);
        return Ok(new { Message = "Utloggad." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await _users.FindByIdAsync(UserId);
        if (user == null) return NotFound();
        return Ok(new UserDto(user.Id, user.Email!, user.FirstName, user.LastName, await GetMembershipsAsync(user.Id)));
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
            .AnyAsync(m => m.BudgetId == membership.BudgetId && m.UserId == UserId && m.Id != membership.Id);
        if (alreadyMember)
            return BadRequest(new { Message = "Du är redan medlem i denna budget." });

        membership.UserId     = UserId;
        membership.AcceptedAt = DateTime.UtcNow;
        membership.InviteToken = null;
        await _db.SaveChangesAsync();

        var user        = await _users.FindByIdAsync(UserId);
        if (user == null) return Unauthorized();
        var memberships = await GetMembershipsAsync(UserId);
        var access      = _tokens.GenerateAccessToken(user, memberships);

        return Ok(new AuthResultDto(access, new UserDto(user.Id, user.Email!, user.FirstName, user.LastName, memberships)));
    }

    private async Task<IActionResult> IssueTokensAsync(ApplicationUser user)
    {
        var memberships    = await GetMembershipsAsync(user.Id);
        var access         = _tokens.GenerateAccessToken(user, memberships);
        var (raw, _)       = await _tokens.GenerateRefreshTokenAsync(user.Id);
        Response.Cookies.Append(CookieName, raw, new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
            Expires  = DateTimeOffset.UtcNow.AddDays(30)
        });
        return Ok(new AuthResultDto(access, new UserDto(user.Id, user.Email!, user.FirstName, user.LastName, memberships)));
    }

    private async Task<List<BudgetMembershipDto>> GetMembershipsAsync(string userId) =>
        await _db.BudgetMemberships
            .Where(m => m.UserId == userId && m.AcceptedAt != null)
            .Include(m => m.Budget)
            .Select(m => new BudgetMembershipDto(m.BudgetId, m.Budget.Name, m.Role))
            .ToListAsync();
}
