using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories.Interfaces;

public interface IProjectRepository
{
    Task<List<(Project Project, decimal SpentAmount)>> GetForBudgetWithSpentAsync(int budgetId);
    Task<Project?>  GetByIdAsync(int id, int budgetId);
    Task<Project>   CreateAsync(Project project);
    Task<Project>   UpdateAsync(Project project);
    Task            DeleteAsync(int id, int budgetId);
}
