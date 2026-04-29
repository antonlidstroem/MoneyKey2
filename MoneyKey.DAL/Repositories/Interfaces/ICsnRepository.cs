using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories.Interfaces;

public interface ICsnRepository
{
    Task<List<CsnEntry>> GetForBudgetAsync(int budgetId, string userId);
    Task<CsnEntry?> GetByIdAsync(int id, int budgetId);
    Task<CsnEntry> CreateAsync(CsnEntry entry);
    Task<CsnEntry> UpdateAsync(CsnEntry entry);
    Task DeleteAsync(int id, int budgetId);
}