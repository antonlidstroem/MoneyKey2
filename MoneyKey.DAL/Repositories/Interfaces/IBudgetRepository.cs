using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories.Interfaces;

public interface IBudgetRepository
{
    Task<List<Budget>>        GetForUserAsync(string userId);
    Task<Budget?>             GetByIdAsync(int id);
    Task<Budget>              CreateAsync(Budget budget);
    Task<Budget>              UpdateAsync(Budget budget);
    Task                      DeleteAsync(int id);
    Task<BudgetMembership?>   GetMembershipAsync(int budgetId, string userId);
    Task<BudgetMembership?>   GetPendingInviteAsync(int budgetId, string email);
    Task<BudgetMembership>    AddMemberAsync(BudgetMembership membership);
    Task<BudgetMembership>    UpdateMemberAsync(BudgetMembership membership);
    Task                      UpdateMemberRoleAsync(int budgetId, string userId, BudgetMemberRole role);
    Task                      RemoveMemberAsync(int budgetId, string userId);
    Task<BudgetMembership?>   GetByInviteTokenAsync(string token);
}
