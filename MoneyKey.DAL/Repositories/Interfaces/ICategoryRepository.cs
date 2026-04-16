using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories.Interfaces;

public interface ICategoryRepository
{
    Task<List<Category>> GetForBudgetAsync(int budgetId);
    Task<Category>       CreateAsync(Category category);
    Task                 DeleteAsync(int id, int budgetId);
}
