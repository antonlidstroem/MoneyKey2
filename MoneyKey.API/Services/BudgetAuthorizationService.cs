using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.API.Services;

public class BudgetAuthorizationService
{
    private readonly BudgetDbContext _db;
    public BudgetAuthorizationService(BudgetDbContext db) => _db = db;

    public async Task<BudgetMembership?> GetMembershipAsync(int budgetId, string userId) =>
        await _db.BudgetMemberships
            .FirstOrDefaultAsync(m => m.BudgetId == budgetId && m.UserId == userId && m.AcceptedAt != null);

    /// <summary>
    /// Role hierarchy (highest → lowest): Owner(4) > Editor(3) > Auditor(2) > Viewer(1).
    /// Returns true when the user's role rank >= minimumRole rank.
    /// </summary>
    public async Task<bool> HasRoleAsync(int budgetId, string userId, BudgetMemberRole minimumRole)
    {
        var m = await GetMembershipAsync(budgetId, userId);
        if (m == null) return false;

        static int Rank(BudgetMemberRole r) => r switch
        {
            BudgetMemberRole.Viewer  => 1,
            BudgetMemberRole.Auditor => 2,
            BudgetMemberRole.Editor  => 3,
            BudgetMemberRole.Owner   => 4,
            _                        => 0
        };

        return Rank(m.Role) >= Rank(minimumRole);
    }
}
