using MoneyKey.Domain.Models;
namespace MoneyKey.DAL.Repositories.Interfaces;
public interface IBudgetTargetRepository {
    Task<List<BudgetTarget>> GetForMonthAsync(int budgetId, int year, int month);
    Task<List<BudgetTarget>> GetForYearAsync(int budgetId, int year);
    Task<BudgetTarget>       UpsertAsync(BudgetTarget t);
    Task                     DeleteAsync(int id, int budgetId);
}
