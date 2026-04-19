using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class TimeEntryRepository : ITimeEntryRepository
{
    private readonly BudgetDbContext _db;
    public TimeEntryRepository(BudgetDbContext db) => _db = db;

    public async Task<List<TimeEntry>> GetForBudgetAsync(int budgetId, int? jobId = null, DateTime? from = null, DateTime? to = null)
    {
        var q = _db.TimeEntries.Include(e => e.Job)
            .Where(e => e.BudgetId == budgetId);
        if (jobId.HasValue) q = q.Where(e => e.JobId == jobId.Value);
        if (from.HasValue)  q = q.Where(e => e.Date >= from.Value);
        if (to.HasValue)    q = q.Where(e => e.Date <= to.Value);
        return await q.OrderByDescending(e => e.Date).ThenByDescending(e => e.StartTime).ToListAsync();
    }

    public async Task<List<TimeEntry>> GetUnpostedAsync(int budgetId, int jobId, string periodKey)
    {
        var parts  = periodKey.Split('-');
        var year   = int.Parse(parts[0]);
        var month  = int.Parse(parts[1]);
        var start  = new DateTime(year, month, 1);
        var end    = start.AddMonths(1).AddDays(-1);
        return await _db.TimeEntries
            .Where(e => e.BudgetId == budgetId && e.JobId == jobId
                     && e.Date >= start && e.Date <= end
                     && e.LinkedTransactionId == null && !e.IsBreak)
            .OrderBy(e => e.Date).ToListAsync();
    }

    public async Task<TimeEntry?> GetByIdAsync(int id, int budgetId) =>
        await _db.TimeEntries.Include(e => e.Job)
            .FirstOrDefaultAsync(e => e.Id == id && e.BudgetId == budgetId);

    public async Task<TimeEntry> CreateAsync(TimeEntry e) { _db.TimeEntries.Add(e); await _db.SaveChangesAsync(); return e; }
    public async Task<TimeEntry> UpdateAsync(TimeEntry e) { _db.TimeEntries.Update(e); await _db.SaveChangesAsync(); return e; }

    public async Task DeleteAsync(int id, int budgetId)
    {
        var e = await GetByIdAsync(id, budgetId);
        if (e != null) { _db.TimeEntries.Remove(e); await _db.SaveChangesAsync(); }
    }

    public async Task MarkPostedAsync(List<int> entryIds, int transactionId, string periodKey)
    {
        var entries = await _db.TimeEntries.Where(e => entryIds.Contains(e.Id)).ToListAsync();
        foreach (var e in entries) { e.LinkedTransactionId = transactionId; e.PayrollPeriodKey = periodKey; }
        await _db.SaveChangesAsync();
    }
}
