using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class BudgetRepository : IBudgetRepository
{
    private readonly BudgetDbContext _db;
    public BudgetRepository(BudgetDbContext db) => _db = db;

    // FIX LOW-2: Filter out nulls in case of orphaned membership rows (no matching Budget).
    public async Task<List<Budget>> GetForUserAsync(string userId) =>
        await _db.BudgetMemberships
            .Where(m => m.UserId == userId && m.AcceptedAt != null)
            .Include(m => m.Budget)
            .Where(m => m.Budget != null)
            .Select(m => m.Budget!)
            .ToListAsync();

    public async Task<Budget?> GetByIdAsync(int id) =>
        await _db.Budgets.Include(b => b.Memberships).FirstOrDefaultAsync(b => b.Id == id);

    public async Task<List<BudgetMembership>> GetMembersAsync(int budgetId)
    {
        return await _db.BudgetMemberships
            .Where(m => m.BudgetId == budgetId)
            .ToListAsync();
    }

    public async Task<Budget> CreateAsync(Budget b)  { _db.Budgets.Add(b); await _db.SaveChangesAsync(); return b; }
    public async Task<Budget> UpdateAsync(Budget b)  { _db.Budgets.Update(b); await _db.SaveChangesAsync(); return b; }

    public async Task DeleteAsync(int id)
    {
        var b = await _db.Budgets.FindAsync(id);
        if (b != null) { _db.Budgets.Remove(b); await _db.SaveChangesAsync(); }
    }

    public async Task<BudgetMembership?> GetMembershipAsync(int budgetId, string userId) =>
        await _db.BudgetMemberships.FirstOrDefaultAsync(m => m.BudgetId == budgetId && m.UserId == userId);

    /// <summary>
    /// Returns a pending (not-yet-accepted) invite for the given email on the given budget.
    /// UserId stores the invitee email as a placeholder until AcceptInvite overwrites it.
    /// </summary>
    public async Task<BudgetMembership?> GetPendingInviteAsync(int budgetId, string email) =>
        await _db.BudgetMemberships
            .FirstOrDefaultAsync(m => m.BudgetId == budgetId
                                   && m.UserId == email
                                   && m.AcceptedAt == null
                                   && m.InviteToken != null);

    public async Task<BudgetMembership> AddMemberAsync(BudgetMembership m)
    {
        _db.BudgetMemberships.Add(m); await _db.SaveChangesAsync(); return m;
    }

    public async Task<BudgetMembership> UpdateMemberAsync(BudgetMembership m)
    {
        _db.BudgetMemberships.Update(m); await _db.SaveChangesAsync(); return m;
    }

    public async Task UpdateMemberRoleAsync(int budgetId, string userId, BudgetMemberRole role)
    {
        var m = await GetMembershipAsync(budgetId, userId);
        if (m != null) { m.Role = role; await _db.SaveChangesAsync(); }
    }

    public async Task RemoveMemberAsync(int budgetId, string userId)
    {
        var m = await GetMembershipAsync(budgetId, userId);
        if (m != null) { _db.BudgetMemberships.Remove(m); await _db.SaveChangesAsync(); }
    }

    public async Task<BudgetMembership?> GetByInviteTokenAsync(string token) =>
        await _db.BudgetMemberships.Include(m => m.Budget)
            .FirstOrDefaultAsync(m => m.InviteToken == token && m.AcceptedAt == null);

    // ── Feature flags ─────────────────────────────────────────────────────────
    // Stored in AppSettings with Key = "Feature_<feature>" and Value = "disabled".

    public async Task<List<string>> GetDisabledFeaturesAsync(int budgetId)
    {
        var prefix  = "Feature_";
        var settings = await _db.AppSettings
            .Where(s => s.BudgetId == budgetId && s.Key.StartsWith(prefix) && s.Value == "disabled")
            .ToListAsync();
        return settings.Select(s => s.Key[prefix.Length..]).ToList();
    }

    public async Task DisableFeatureAsync(int budgetId, string feature)
    {
        var key     = $"Feature_{feature}";
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.BudgetId == budgetId && s.Key == key);
        if (setting == null)
            _db.AppSettings.Add(new AppSetting { BudgetId = budgetId, Key = key, Value = "disabled" });
        else
            setting.Value = "disabled";
        await _db.SaveChangesAsync();
    }

    public async Task EnableFeatureAsync(int budgetId, string feature)
    {
        var key     = $"Feature_{feature}";
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.BudgetId == budgetId && s.Key == key);
        if (setting != null) { _db.AppSettings.Remove(setting); await _db.SaveChangesAsync(); }
    }

}