using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Models;

namespace MoneyKey.Core.Services;

/// <summary>
/// GDPR Article 17 — Right to Erasure.
/// Pseudonymises user data rather than hard-deleting to preserve budget integrity
/// for other members. All personal identifiers are replaced with a placeholder.
/// </summary>
public class AccountDeletionService
{
    private readonly BudgetDbContext           _db;
    private readonly UserManager<ApplicationUser> _users;

    public AccountDeletionService(BudgetDbContext db, UserManager<ApplicationUser> users)
    { _db = db; _users = users; }

    public async Task<bool> DeleteAccountAsync(string userId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var placeholder = $"[borttagen-{userId[..8]}]";

            // 1. Pseudonymise transaction descriptions that contain user-specific data
            //    (descriptions written by this user — keep financial amounts intact)
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE Transactions SET Description = CASE WHEN Description LIKE '%@%' THEN '[borttagen]' ELSE Description END WHERE CreatedByUserId = {0}", userId);
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE Transactions SET CreatedByUserId = {0} WHERE CreatedByUserId = {1}", placeholder, userId);

            // 2. Remove private lists and notes
            var privateLists = await _db.UserLists
                .Where(l => l.CreatedByUserId == userId && l.Visibility == MoneyKey.Domain.Enums.EntryVisibility.Private)
                .ToListAsync();
            _db.UserLists.RemoveRange(privateLists);

            // 3. Pseudonymise shared lists (keep for other members)
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE UserLists SET CreatedByUserId = {0} WHERE CreatedByUserId = {1}", placeholder, userId);

            // 4. Remove time entries (personal work data)
            var timeEntries = await _db.TimeEntries.Where(e => e.UserId == userId).ToListAsync();
            _db.TimeEntries.RemoveRange(timeEntries);

            // 5. Remove milersättning entries
            var miEntries = await _db.MilersattningEntries.Where(e => e.UserId == userId).ToListAsync();
            _db.MilersattningEntries.RemoveRange(miEntries);

            // 6. Remove VAB entries
            var vabEntries = await _db.VabEntries.Where(e => e.UserId == userId).ToListAsync();
            _db.VabEntries.RemoveRange(vabEntries);

            // 7. Remove budgets where user is sole member (no other accepted members)
            var soloMemberships = await _db.Set<MoneyKey.Domain.Models.BudgetMembership>()
                .Where(m => m.UserId == userId)
                .ToListAsync();
            foreach (var m in soloMemberships)
            {
                var otherMembers = await _db.Set<MoneyKey.Domain.Models.BudgetMembership>()
                    .CountAsync(x => x.BudgetId == m.BudgetId && x.UserId != userId && x.AcceptedAt != null);
                if (otherMembers == 0)
                {
                    // Transfer or delete the budget
                    var budget = await _db.Budgets.FindAsync(m.BudgetId);
                    if (budget != null) _db.Budgets.Remove(budget);
                }
            }

            // 8. Remove memberships
            _db.Set<MoneyKey.Domain.Models.BudgetMembership>().RemoveRange(soloMemberships);

            // 9. Remove subscription
            var sub = await _db.UserSubscriptions.FindAsync(userId);
            if (sub != null) _db.UserSubscriptions.Remove(sub);

            // 10. Remove pending invitations
            var invitations = await _db.BudgetInvitations
                .Where(i => i.InvitedUserId == userId || i.InvitedByUserId == userId)
                .ToListAsync();
            _db.BudgetInvitations.RemoveRange(invitations);

            // 11. Remove refresh tokens
            var tokens = await _db.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
            _db.RefreshTokens.RemoveRange(tokens);

            await _db.SaveChangesAsync();

            // 12. Delete the Identity user (removes email, password hash, etc.)
            var user = await _users.FindByIdAsync(userId);
            if (user != null) await _users.DeleteAsync(user);

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
