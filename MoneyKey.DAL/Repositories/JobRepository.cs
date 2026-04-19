using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class JobRepository : IJobRepository
{
    private readonly BudgetDbContext _db;
    public JobRepository(BudgetDbContext db) => _db = db;

    public async Task<List<Job>> GetForBudgetAsync(int budgetId, bool includeInactive = false)
    {
        var q = _db.Jobs.Include(j => j.Project).Where(j => j.BudgetId == budgetId);
        if (!includeInactive) q = q.Where(j => j.IsActive);
        return await q.OrderBy(j => j.Name).ToListAsync();
    }

    public async Task<Job?> GetByIdAsync(int id, int budgetId) =>
        await _db.Jobs.Include(j => j.Project)
            .FirstOrDefaultAsync(j => j.Id == id && j.BudgetId == budgetId);

    public async Task<Job> CreateAsync(Job j) { _db.Jobs.Add(j); await _db.SaveChangesAsync(); return j; }
    public async Task<Job> UpdateAsync(Job j) { _db.Jobs.Update(j); await _db.SaveChangesAsync(); return j; }

    public async Task DeleteAsync(int id, int budgetId)
    {
        var j = await GetByIdAsync(id, budgetId);
        if (j != null) { _db.Jobs.Remove(j); await _db.SaveChangesAsync(); }
    }
}
