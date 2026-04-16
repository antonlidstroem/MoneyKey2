using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class SystemSettingRepository : ISystemSettingRepository
{
    private readonly BudgetDbContext _db;
    public SystemSettingRepository(BudgetDbContext db) => _db = db;

    public async Task<string?> GetAsync(string key) =>
        (await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == key))?.Value;

    public async Task SetAsync(string key, string value)
    {
        var s = await _db.SystemSettings.FirstOrDefaultAsync(x => x.Key == key);
        if (s == null) _db.SystemSettings.Add(new SystemSetting { Key = key, Value = value });
        else s.Value = value;
        await _db.SaveChangesAsync();
    }
}
