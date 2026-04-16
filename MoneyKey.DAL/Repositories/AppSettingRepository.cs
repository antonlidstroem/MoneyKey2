using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class AppSettingRepository : IAppSettingRepository
{
    private readonly BudgetDbContext _db;
    public AppSettingRepository(BudgetDbContext db) => _db = db;

    public async Task<string?> GetAsync(int budgetId, string key) =>
        (await _db.AppSettings.FirstOrDefaultAsync(s => s.BudgetId == budgetId && s.Key == key))?.Value;

    public async Task SetAsync(int budgetId, string key, string value)
    {
        var s = await _db.AppSettings.FirstOrDefaultAsync(x => x.BudgetId == budgetId && x.Key == key);
        if (s == null) _db.AppSettings.Add(new AppSetting { BudgetId = budgetId, Key = key, Value = value });
        else s.Value = value;
        await _db.SaveChangesAsync();
    }
}
