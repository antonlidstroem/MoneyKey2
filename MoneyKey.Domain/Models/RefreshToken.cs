namespace MoneyKey.Domain.Models;

public class RefreshToken
{
    public int       Id              { get; set; }
    public string    UserId          { get; set; } = string.Empty;
    public string    TokenHash       { get; set; } = string.Empty;
    public DateTime  ExpiresAt       { get; set; }
    public DateTime  CreatedAt       { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt       { get; set; }
    public string?   ReplacedByToken { get; set; }
    public bool IsExpired            => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked            => RevokedAt.HasValue;
    public bool IsActive             => !IsRevoked && !IsExpired;
}
