using MoneyKey.Domain.Models;
namespace MoneyKey.DAL.Repositories.Interfaces;
public interface ISickLeaveRepository {
    Task<List<SickLeaveEntry>> GetForBudgetAsync(int budgetId, string? userId = null);
    Task<SickLeaveEntry?>      GetByIdAsync(int id, int budgetId);
    Task<SickLeaveEntry>       CreateAsync(SickLeaveEntry e);
    Task<SickLeaveEntry>       UpdateAsync(SickLeaveEntry e);
    Task                       DeleteAsync(int id, int budgetId);
}
