using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Models;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class UserSubscriptionRepository : IUserSubscriptionRepository
{
    private readonly BudgetDbContext          _db;
    private readonly UserManager<ApplicationUser> _users;

    public UserSubscriptionRepository(BudgetDbContext db, UserManager<ApplicationUser> users)
    { _db = db; _users = users; }

    public async Task<UserSubscription?> GetAsync(string userId) =>
        await _db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId);

    public async Task<UserSubscription> UpsertAsync(UserSubscription sub)
    {
        var existing = await GetAsync(sub.UserId);
        if (existing == null) { sub.CreatedAt = DateTime.UtcNow; _db.UserSubscriptions.Add(sub); }
        else
        {
            existing.Tier       = sub.Tier;
            existing.PaidUntil  = sub.PaidUntil;
            existing.PaymentRef = sub.PaymentRef;
            existing.IsAdmin    = sub.IsAdmin;
            existing.AdminNotes = sub.AdminNotes;
            existing.UpdatedAt  = DateTime.UtcNow;
            _db.UserSubscriptions.Update(existing);
        }
        await _db.SaveChangesAsync();
        return existing ?? sub;
    }

    public async Task<List<(string UserId, string Email, string DisplayName, UserSubscription Sub)>>
        GetAllForAdminAsync(string? search = null, int page = 1, int pageSize = 50)
    {
        var usersQ = _users.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            usersQ = usersQ.Where(u => u.Email!.Contains(search) || u.DisplayName.Contains(search));

        var users = await usersQ
            .OrderBy(u => u.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();
        var subs    = await _db.UserSubscriptions
            .Where(s => userIds.Contains(s.UserId))
            .ToListAsync();

        return users.Select(u =>
        {
            var sub = subs.FirstOrDefault(s => s.UserId == u.Id)
                   ?? new UserSubscription { UserId = u.Id };
            return (u.Id, u.Email ?? "", u.DisplayName, sub);
        }).ToList();
    }

    public Task<int> CountAsync() => _users.Users.CountAsync();

    public async Task<string?> FindUserIdByDisplayNameAsync(string displayName) =>
        (await _users.Users
            .FirstOrDefaultAsync(u => u.DisplayName.ToLower() == displayName.ToLower()))?.Id;

    public async Task<List<(string UserId, string DisplayName)>> SearchByDisplayNameAsync(string prefix, int limit = 8)
    {
        var results = await _users.Users
            .Where(u => u.DisplayName.StartsWith(prefix))
            .Take(limit)
            .Select(u => new { u.Id, u.DisplayName })
            .ToListAsync();
        return results.Select(r => (r.Id, r.DisplayName)).ToList();
    }
}
