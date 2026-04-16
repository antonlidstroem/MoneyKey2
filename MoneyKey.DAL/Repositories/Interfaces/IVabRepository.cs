using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories.Interfaces;

public interface IVabRepository
{
    Task<List<VabEntry>> GetForBudgetAsync(int budgetId, string? userId = null);
    Task<VabEntry?>      GetByIdAsync(int id, int budgetId);
    Task<VabEntry>       CreateAsync(VabEntry entry);
    Task<VabEntry>       UpdateAsync(VabEntry entry);
    Task                 DeleteAsync(int id, int budgetId);
}
