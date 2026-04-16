namespace MoneyKey.DAL.Data.Interfaces;

public interface ICurrentUserAccessor
{
    string? UserId    { get; }
    string? UserEmail { get; }
}
