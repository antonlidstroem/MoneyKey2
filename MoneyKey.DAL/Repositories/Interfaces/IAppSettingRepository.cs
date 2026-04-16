namespace MoneyKey.DAL.Repositories.Interfaces;

public interface IAppSettingRepository
{
    Task<string?> GetAsync(int budgetId, string key);
    Task          SetAsync(int budgetId, string key, string value);
}
