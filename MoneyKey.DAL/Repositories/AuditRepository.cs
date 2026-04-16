using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly BudgetDbContext _db;
    public AuditRepository(BudgetDbContext db) => _db = db;

    public async Task LogAsync(AuditLog entry) { _db.AuditLogs.Add(entry); await _db.SaveChangesAsync(); }

    public async Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(int budgetId, int page, int pageSize)
    {
        var q     = _db.AuditLogs.Where(a => a.BudgetId == budgetId).OrderByDescending(a => a.Timestamp);
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }
}
