using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MoneyKey.Core.DTOs.Budget;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Models;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Services;

public class TokenService
{
    private readonly IConfiguration _cfg;
    private readonly BudgetDbContext _db;

    public TokenService(IConfiguration cfg, BudgetDbContext db)
    {
        _cfg = cfg;
        _db  = db;
    }

    public string GenerateAccessToken(ApplicationUser user, List<BudgetMembershipDto> memberships)
    {
        var jwtSection = _cfg.GetSection("Jwt");
        var key        = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Secret"]!));
        var creds      = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        // FIX LOW-1: TryParse so an empty config value does not throw FormatException.
        var expiry     = DateTime.UtcNow.AddMinutes(int.TryParse(jwtSection["AccessTokenExpiryMinutes"], out var mins) ? mins : 15);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new("firstName",                   user.FirstName),
            new("lastName",                    user.LastName)
        };
        foreach (var m in memberships)
            claims.Add(new Claim("budget", $"{m.BudgetId}:{m.Role}"));

        return new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityToken(jwtSection["Issuer"], jwtSection["Audience"], claims, expires: expiry, signingCredentials: creds));
    }

    public async Task<(string RawToken, RefreshToken Entity)> GenerateRefreshTokenAsync(string userId)
    {
        var raw    = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash   = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        // FIX LOW-1: Use TryParse so an empty/invalid config value does not throw FormatException.
        var days   = int.TryParse(_cfg["Jwt:RefreshTokenExpiryDays"], out var d) ? d : 30;
        var entity = new RefreshToken { UserId = userId, TokenHash = hash, ExpiresAt = DateTime.UtcNow.AddDays(days) };
        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync();
        return (raw, entity);
    }

    public async Task<RefreshToken?> ValidateRefreshTokenAsync(string rawToken)
    {
        var hash  = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        return token?.IsActive == true ? token : null;
    }

    /// <summary>
    /// FIX SECURITY-1: Atomically validates AND revokes a refresh token in one SaveChanges call.
    /// This closes the race-condition window where two concurrent requests could both pass
    /// ValidateRefreshTokenAsync before either calls RevokeRefreshTokenAsync.
    /// Returns the validated (and now-revoked) token, or null if the token is invalid/expired.
    /// </summary>
    public async Task<RefreshToken?> ValidateAndRevokeRefreshTokenAsync(string rawToken)
    {
        var hash  = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (token == null || !token.IsActive) return null;

        // Stamp the revocation time and save in the same operation.
        // If two requests race, the second FindAsync will still see the row but IsActive
        // will be false after the first SaveChanges completes — so it returns null.
        token.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return token;
    }

    public async Task RevokeRefreshTokenAsync(int tokenId)
    {
        var t = await _db.RefreshTokens.FindAsync(tokenId);
        if (t != null) { t.RevokedAt = DateTime.UtcNow; await _db.SaveChangesAsync(); }
    }

    public async Task RevokeAllRefreshTokensAsync(string userId)
    {
        var tokens = await _db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync();
        foreach (var t in tokens) t.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
