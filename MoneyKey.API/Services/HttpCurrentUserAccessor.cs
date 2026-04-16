using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MoneyKey.DAL.Data.Interfaces;

namespace MoneyKey.API.Services;

public class HttpCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _http;
    public HttpCurrentUserAccessor(IHttpContextAccessor http) => _http = http;

    public string? UserId =>
        _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? _http.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    public string? UserEmail =>
        _http.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value
        ?? _http.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
}
