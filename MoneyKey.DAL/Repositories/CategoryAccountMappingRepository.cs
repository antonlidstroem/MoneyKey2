using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;
namespace MoneyKey.DAL.Repositories;
public class CategoryAccountMappingRepository : ICategoryAccountMappingRepository {
    private readonly BudgetDbContext _db;
    public CategoryAccountMappingRepository(BudgetDbContext db) => _db = db;
    public Task<List<CategoryAccountMapping>> GetForBudgetAsync(int budgetId) =>
        _db.CategoryAccountMappings.Include(m => m.Category).Where(m => m.BudgetId == budgetId).ToListAsync();
    public async Task<CategoryAccountMapping> UpsertAsync(CategoryAccountMapping m) {
        var ex = await _db.CategoryAccountMappings.FirstOrDefaultAsync(x => x.BudgetId == m.BudgetId && x.CategoryId == m.CategoryId);
        if (ex == null) { _db.CategoryAccountMappings.Add(m); }
        else { ex.BasAccount = m.BasAccount; ex.AccountName = m.AccountName; }
        await _db.SaveChangesAsync();
        return ex ?? m;
    }
    public async Task DeleteAsync(int id, int budgetId) {
        var m = await _db.CategoryAccountMappings.FirstOrDefaultAsync(x => x.Id == id && x.BudgetId == budgetId);
        if (m != null) { _db.CategoryAccountMappings.Remove(m); await _db.SaveChangesAsync(); }
    }
}
