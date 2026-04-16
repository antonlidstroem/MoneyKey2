using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class VabRepository : IVabRepository
{
    private readonly BudgetDbContext _db;
    public VabRepository(BudgetDbContext db) => _db = db;

    public async Task<List<VabEntry>> GetForBudgetAsync(int budgetId, string? userId = null)
    {
        var q = _db.VabEntries.Where(e => e.BudgetId == budgetId);
        if (userId != null) q = q.Where(e => e.UserId == userId);
        return await q.OrderByDescending(e => e.StartDate).ToListAsync();
    }

    public async Task<VabEntry?> GetByIdAsync(int id, int budgetId) =>
        await _db.VabEntries.FirstOrDefaultAsync(e => e.Id == id && e.BudgetId == budgetId);

    public async Task<VabEntry> CreateAsync(VabEntry e) { _db.VabEntries.Add(e); await _db.SaveChangesAsync(); return e; }
    public async Task<VabEntry> UpdateAsync(VabEntry e) { _db.VabEntries.Update(e); await _db.SaveChangesAsync(); return e; }

    public async Task DeleteAsync(int id, int budgetId)
    {
        var e = await GetByIdAsync(id, budgetId);
        if (e != null) { _db.VabEntries.Remove(e); await _db.SaveChangesAsync(); }
    }
}
