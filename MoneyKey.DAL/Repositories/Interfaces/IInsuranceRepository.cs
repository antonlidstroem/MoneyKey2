using MoneyKey.Domain.Models;
namespace MoneyKey.DAL.Repositories.Interfaces;
public interface IInsuranceRepository {
    Task<List<Insurance>> GetForBudgetAsync(int budgetId, bool includeInactive = false);
    Task<Insurance?>      GetByIdAsync(int id, int budgetId);
    Task<Insurance>       CreateAsync(Insurance i);
    Task<Insurance>       UpdateAsync(Insurance i);
    Task                  DeleteAsync(int id, int budgetId);
}
