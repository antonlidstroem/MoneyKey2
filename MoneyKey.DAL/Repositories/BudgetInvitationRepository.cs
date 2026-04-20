using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class BudgetInvitationRepository : IBudgetInvitationRepository
{
    private readonly BudgetDbContext _db;
    public BudgetInvitationRepository(BudgetDbContext db) => _db = db;

    public async Task<List<BudgetInvitation>> GetPendingForUserAsync(string userId) =>
        await _db.BudgetInvitations
            .Include(i => i.Budget)
            .Where(i => i.InvitedUserId == userId
                     && i.Status == BudgetInvitationStatus.Pending
                     && i.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

    public async Task<List<BudgetInvitation>> GetForBudgetAsync(int budgetId) =>
        await _db.BudgetInvitations
            .Where(i => i.BudgetId == budgetId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

    public Task<BudgetInvitation?> GetByIdAsync(int id) =>
        _db.BudgetInvitations.Include(i => i.Budget).FirstOrDefaultAsync(i => i.Id == id);

    public async Task<BudgetInvitation> CreateAsync(BudgetInvitation inv)
    { _db.BudgetInvitations.Add(inv); await _db.SaveChangesAsync(); return inv; }

    public async Task UpdateStatusAsync(int id, BudgetInvitationStatus status)
    {
        var inv = await _db.BudgetInvitations.FindAsync(id);
        if (inv != null) { inv.Status = status; await _db.SaveChangesAsync(); }
    }

    public async Task<bool> HasPendingAsync(int budgetId, string invitedUserId) =>
        await _db.BudgetInvitations.AnyAsync(i =>
            i.BudgetId == budgetId && i.InvitedUserId == invitedUserId
         && i.Status == BudgetInvitationStatus.Pending && i.ExpiresAt > DateTime.UtcNow);
}
