using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories.Interfaces;

public interface IBudgetInvitationRepository
{
    Task<List<BudgetInvitation>> GetPendingForUserAsync(string userId);
    Task<List<BudgetInvitation>> GetForBudgetAsync(int budgetId);
    Task<BudgetInvitation?>      GetByIdAsync(int id);
    Task<BudgetInvitation>       CreateAsync(BudgetInvitation inv);
    Task                         UpdateStatusAsync(int id, BudgetInvitationStatus status);
    Task<bool>                   HasPendingAsync(int budgetId, string invitedUserId);
}
