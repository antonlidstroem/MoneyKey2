using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;
namespace MoneyKey.DAL.Repositories;
public class InsuranceRepository : IInsuranceRepository {
    private readonly BudgetDbContext _db;
    public InsuranceRepository(BudgetDbContext db) => _db = db;
    public async Task<List<Insurance>> GetForBudgetAsync(int budgetId, bool includeInactive = false) {
        var q = _db.Insurances.Where(i => i.BudgetId == budgetId);
        if (!includeInactive) q = q.Where(i => i.IsActive);
        return await q.OrderBy(i => i.Name).ToListAsync();
    }
    public Task<Insurance?> GetByIdAsync(int id, int budgetId) =>
        _db.Insurances.FirstOrDefaultAsync(i => i.Id == id && i.BudgetId == budgetId);
    public async Task<Insurance> CreateAsync(Insurance i) { _db.Insurances.Add(i); await _db.SaveChangesAsync(); return i; }
    public async Task<Insurance> UpdateAsync(Insurance i) { _db.Insurances.Update(i); await _db.SaveChangesAsync(); return i; }
    public async Task DeleteAsync(int id, int budgetId) {
        var i = await GetByIdAsync(id, budgetId);
        if (i != null) { _db.Insurances.Remove(i); await _db.SaveChangesAsync(); }
    }
}
