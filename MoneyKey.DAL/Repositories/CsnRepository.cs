using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class CsnRepository : ICsnRepository
{
    private readonly BudgetDbContext _db;
    public CsnRepository(BudgetDbContext db) => _db = db;

    public Task<List<CsnEntry>> GetForBudgetAsync(int budgetId, string userId) =>
        _db.CsnEntries
           .Where(e => e.BudgetId == budgetId && e.UserId == userId)
           .OrderByDescending(e => e.Year)
           .ToListAsync();

    public Task<CsnEntry?> GetByIdAsync(int id, int budgetId) =>
        _db.CsnEntries.FirstOrDefaultAsync(e => e.Id == id && e.BudgetId == budgetId);

    public async Task<CsnEntry> CreateAsync(CsnEntry entry)
    {
        _db.CsnEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task<CsnEntry> UpdateAsync(CsnEntry entry)
    {
        entry.UpdatedAt = DateTime.UtcNow;
        _db.CsnEntries.Update(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task DeleteAsync(int id, int budgetId)
    {
        var e = await GetByIdAsync(id, budgetId);
        if (e != null) { _db.CsnEntries.Remove(e); await _db.SaveChangesAsync(); }
    }
}