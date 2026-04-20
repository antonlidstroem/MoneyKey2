using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;
namespace MoneyKey.DAL.Repositories;
public class LoanRepository : ILoanRepository {
    private readonly BudgetDbContext _db;
    public LoanRepository(BudgetDbContext db) => _db = db;
    public async Task<List<Loan>> GetForBudgetAsync(int budgetId, bool includeInactive = false) {
        var q = _db.Loans.Where(l => l.BudgetId == budgetId);
        if (!includeInactive) q = q.Where(l => l.IsActive);
        return await q.OrderBy(l => l.Name).ToListAsync();
    }
    public Task<Loan?> GetByIdAsync(int id, int budgetId) =>
        _db.Loans.FirstOrDefaultAsync(l => l.Id == id && l.BudgetId == budgetId);
    public async Task<Loan> CreateAsync(Loan l) { _db.Loans.Add(l); await _db.SaveChangesAsync(); return l; }
    public async Task<Loan> UpdateAsync(Loan l) { _db.Loans.Update(l); await _db.SaveChangesAsync(); return l; }
    public async Task DeleteAsync(int id, int budgetId) {
        var l = await GetByIdAsync(id, budgetId);
        if (l != null) { _db.Loans.Remove(l); await _db.SaveChangesAsync(); }
    }
}
