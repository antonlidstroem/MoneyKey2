using MoneyKey.Domain.Models;
namespace MoneyKey.DAL.Repositories.Interfaces;
public interface ILoanRepository {
    Task<List<Loan>> GetForBudgetAsync(int budgetId, bool includeInactive = false);
    Task<Loan?>      GetByIdAsync(int id, int budgetId);
    Task<Loan>       CreateAsync(Loan l);
    Task<Loan>       UpdateAsync(Loan l);
    Task             DeleteAsync(int id, int budgetId);
}
