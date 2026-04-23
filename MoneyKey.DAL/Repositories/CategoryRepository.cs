using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly BudgetDbContext _db;
    public CategoryRepository(BudgetDbContext db) => _db = db;

    /// <summary>
    /// Returns categories available for user selection in transaction forms.
    /// System categories with IsUserSelectable=false (e.g. Löneinbetalning) are excluded.
    /// Budget-specific custom categories are always included.
    /// </summary>
    public async Task<List<Category>> GetForBudgetAsync(int budgetId) =>
        await _db.Categories
            .Where(c => (c.IsSystemCategory && c.IsUserSelectable)
                     || (!c.IsSystemCategory && c.BudgetId == budgetId))
            .OrderBy(c => c.Type).ThenBy(c => c.Name)
            .ToListAsync();

    public async Task<Category> CreateAsync(Category c) { _db.Categories.Add(c); await _db.SaveChangesAsync(); return c; }

    public async Task DeleteAsync(int id, int budgetId)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id && x.BudgetId == budgetId && !x.IsSystemCategory);
        if (c != null) { _db.Categories.Remove(c); await _db.SaveChangesAsync(); }
    }
}