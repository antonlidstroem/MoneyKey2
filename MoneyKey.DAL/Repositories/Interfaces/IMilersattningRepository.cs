using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories.Interfaces;

public interface IMilersattningRepository
{
    Task<List<MilersattningEntry>> GetForBudgetAsync(int budgetId, string? userId = null);
    Task<MilersattningEntry?>      GetByIdAsync(int id, int budgetId);
    Task<MilersattningEntry>       CreateAsync(MilersattningEntry entry);
    Task<MilersattningEntry>       UpdateAsync(MilersattningEntry entry);
    Task                           DeleteAsync(int id, int budgetId);
}
