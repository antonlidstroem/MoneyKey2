namespace MoneyKey.DAL.Repositories.Interfaces;

/// <summary>
/// Global system settings with no budget scope (no FK to Budgets table).
/// </summary>
public interface ISystemSettingRepository
{
    Task<string?> GetAsync(string key);
    Task          SetAsync(string key, string value);
}
