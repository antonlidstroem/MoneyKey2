using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly BudgetDbContext _db;
    public ProjectRepository(BudgetDbContext db) => _db = db;

    public async Task<List<(Project Project, decimal SpentAmount)>> GetForBudgetWithSpentAsync(int budgetId)
    {
        var projects = await _db.Projects
            .Where(p => p.BudgetId == budgetId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                Project = p,
                SpentAmount = _db.Transactions
                    .Where(t => t.ProjectId == p.Id && t.BudgetId == budgetId)
                    .Sum(t => (decimal?)t.NetAmount) ?? 0m
            })
            .ToListAsync();

        return projects.Select(x => (x.Project, x.SpentAmount)).ToList();
    }

    public async Task<Project?> GetByIdAsync(int id, int budgetId) =>
        await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.BudgetId == budgetId);

    public async Task<Project> CreateAsync(Project p) { _db.Projects.Add(p); await _db.SaveChangesAsync(); return p; }
    public async Task<Project> UpdateAsync(Project p) { _db.Projects.Update(p); await _db.SaveChangesAsync(); return p; }

    public async Task DeleteAsync(int id, int budgetId)
    {
        var p = await GetByIdAsync(id, budgetId);
        if (p != null) { _db.Projects.Remove(p); await _db.SaveChangesAsync(); }
    }
}
