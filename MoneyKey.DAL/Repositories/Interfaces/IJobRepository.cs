using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories.Interfaces;

public interface IJobRepository
{
    Task<List<Job>> GetForBudgetAsync(int budgetId, bool includeInactive = false);
    Task<Job?>      GetByIdAsync(int id, int budgetId);
    Task<Job>       CreateAsync(Job job);
    Task<Job>       UpdateAsync(Job job);
    Task            DeleteAsync(int id, int budgetId);
}
