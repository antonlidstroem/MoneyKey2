using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;
namespace MoneyKey.DAL.Repositories;
public class BudgetTargetRepository : IBudgetTargetRepository {
    private readonly BudgetDbContext _db;
    public BudgetTargetRepository(BudgetDbContext db) => _db = db;
    public Task<List<BudgetTarget>> GetForMonthAsync(int budgetId, int year, int month) =>
        _db.BudgetTargets.Include(t => t.Category).Where(t => t.BudgetId == budgetId && t.Year == year && t.Month == month).ToListAsync();
    public Task<List<BudgetTarget>> GetForYearAsync(int budgetId, int year) =>
        _db.BudgetTargets.Include(t => t.Category).Where(t => t.BudgetId == budgetId && t.Year == year).ToListAsync();
    public async Task<BudgetTarget> UpsertAsync(BudgetTarget t) {
        var existing = await _db.BudgetTargets.FirstOrDefaultAsync(x => x.BudgetId == t.BudgetId && x.CategoryId == t.CategoryId && x.Year == t.Year && x.Month == t.Month);
        if (existing == null) { _db.BudgetTargets.Add(t); }
        else { existing.TargetAmount = t.TargetAmount; existing.Notes = t.Notes; }
        await _db.SaveChangesAsync();
        return existing ?? t;
    }
    public async Task DeleteAsync(int id, int budgetId) {
        var t = await _db.BudgetTargets.FirstOrDefaultAsync(x => x.Id == id && x.BudgetId == budgetId);
        if (t != null) { _db.BudgetTargets.Remove(t); await _db.SaveChangesAsync(); }
    }
}
