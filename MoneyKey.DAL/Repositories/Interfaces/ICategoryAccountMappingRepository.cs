using MoneyKey.Domain.Models;
namespace MoneyKey.DAL.Repositories.Interfaces;
public interface ICategoryAccountMappingRepository {
    Task<List<CategoryAccountMapping>> GetForBudgetAsync(int budgetId);
    Task<CategoryAccountMapping>       UpsertAsync(CategoryAccountMapping m);
    Task                               DeleteAsync(int id, int budgetId);
}
