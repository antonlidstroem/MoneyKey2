using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class MilersattningRepository : IMilersattningRepository
{
    private readonly BudgetDbContext _db;
    public MilersattningRepository(BudgetDbContext db) => _db = db;

    public async Task<List<MilersattningEntry>> GetForBudgetAsync(int budgetId, string? userId = null)
    {
        var q = _db.MilersattningEntries.Where(e => e.BudgetId == budgetId);
        if (userId != null) q = q.Where(e => e.UserId == userId);
        return await q.OrderByDescending(e => e.TripDate).ToListAsync();
    }

    public async Task<MilersattningEntry?> GetByIdAsync(int id, int budgetId) =>
        await _db.MilersattningEntries.FirstOrDefaultAsync(e => e.Id == id && e.BudgetId == budgetId);

    public async Task<MilersattningEntry> CreateAsync(MilersattningEntry e) { _db.MilersattningEntries.Add(e); await _db.SaveChangesAsync(); return e; }
    public async Task<MilersattningEntry> UpdateAsync(MilersattningEntry e) { _db.MilersattningEntries.Update(e); await _db.SaveChangesAsync(); return e; }

    public async Task DeleteAsync(int id, int budgetId)
    {
        var e = await GetByIdAsync(id, budgetId);
        if (e != null) { _db.MilersattningEntries.Remove(e); await _db.SaveChangesAsync(); }
    }
}
