using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Queries;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly BudgetDbContext _db;
    public TransactionRepository(BudgetDbContext db) => _db = db;

    public async Task<(List<Transaction> Items, int TotalCount)> GetPagedAsync(TransactionQuery q)
    {
        var query = _db.Transactions
            .Include(t => t.Category)
            .Include(t => t.Project)
            .Include(t => t.KonteringRows)
            .Where(t => t.BudgetId == q.BudgetId);

        if (q.FilterByStartDate   && q.StartDate.HasValue)   query = query.Where(t => t.StartDate >= q.StartDate.Value);
        if (q.FilterByEndDate     && q.EndDate.HasValue)      query = query.Where(t => t.EndDate == null || t.EndDate <= q.EndDate.Value);
        if (q.FilterByDescription && !string.IsNullOrWhiteSpace(q.Description))
            query = query.Where(t => t.Description != null && t.Description.Contains(q.Description));
        if (q.FilterByAmount      && q.Amount.HasValue)       query = query.Where(t => t.NetAmount == q.Amount.Value);
        if (q.FilterByCategory    && q.CategoryId.HasValue)   query = query.Where(t => t.CategoryId == q.CategoryId.Value);
        if (q.FilterByRecurrence  && q.Recurrence.HasValue)   query = query.Where(t => t.Recurrence == q.Recurrence.Value);
        if (q.FilterByMonth       && q.Month.HasValue)        query = query.Where(t => t.Month == q.Month.Value);
        if (q.ProjectId.HasValue) query = query.Where(t => t.ProjectId == q.ProjectId.Value);
        if (q.Type.HasValue)      query = query.Where(t => t.Type == q.Type.Value);
        if (q.IsActive.HasValue)  query = query.Where(t => t.IsActive == q.IsActive.Value);
        // Exclude auto-generated linked transactions so Milersättning/VAB don't appear twice
        if (q.ExcludeLinked)      query = query.Where(t => t.MilersattningEntryId == null && t.VabEntryId == null);

        var total = await query.CountAsync();

        query = (q.SortBy?.ToLower(), q.SortDir?.ToLower()) switch
        {
            // Primary canonical names used by the direct transactions endpoint
            ("startdate",   "asc") => query.OrderBy(t => t.StartDate),
            ("startdate",   _)     => query.OrderByDescending(t => t.StartDate),
            // Aliases used by JournalFilterState ("Date") and other callers
            ("date",        "asc") => query.OrderBy(t => t.StartDate),
            ("date",        _)     => query.OrderByDescending(t => t.StartDate),
            ("netamount",   "asc") => query.OrderBy(t => t.NetAmount),
            ("netamount",   _)     => query.OrderByDescending(t => t.NetAmount),
            ("amount",      "asc") => query.OrderBy(t => t.NetAmount),
            ("amount",      _)     => query.OrderByDescending(t => t.NetAmount),
            ("description", "asc") => query.OrderBy(t => t.Description),
            ("description", _)     => query.OrderByDescending(t => t.Description),
            ("category",    "asc") => query.OrderBy(t => t.Category.Name),
            ("category",    _)     => query.OrderByDescending(t => t.Category.Name),
            _                      => query.OrderByDescending(t => t.StartDate)
        };

        var items = await query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync();
        return (items, total);
    }

    public async Task<Transaction?> GetByIdAsync(int id, int budgetId) =>
        await _db.Transactions
            .Include(t => t.Category)
            .Include(t => t.Project)
            .Include(t => t.KonteringRows)
            .FirstOrDefaultAsync(t => t.Id == id && t.BudgetId == budgetId);

    public async Task<Transaction> CreateAsync(Transaction t)
    {
        _db.Transactions.Add(t);
        await _db.SaveChangesAsync();
        return t;
    }

    public async Task<Transaction> UpdateAsync(Transaction t)
    {
        t.UpdatedAt = DateTime.UtcNow;
        _db.Transactions.Update(t);
        await _db.SaveChangesAsync();
        return t;
    }

    public async Task DeleteAsync(int id, int budgetId)
    {
        var t = await _db.Transactions.FirstOrDefaultAsync(x => x.Id == id && x.BudgetId == budgetId);
        if (t != null) { _db.Transactions.Remove(t); await _db.SaveChangesAsync(); }
    }

    public async Task DeleteBatchAsync(List<int> ids, int budgetId)
    {
        var txs = await _db.Transactions.Where(t => ids.Contains(t.Id) && t.BudgetId == budgetId).ToListAsync();
        _db.Transactions.RemoveRange(txs);
        await _db.SaveChangesAsync();
    }

    public async Task<List<Transaction>> GetForExportAsync(TransactionQuery q)
    {
        // Clone pagination fields into a new query so the caller's object is not mutated.
        // Mutating q.Page/q.PageSize would silently destroy paging state in callers that
        // reuse the same TransactionQuery instance (e.g. the Export buttons on JournalPage).
        var exportQuery = new TransactionQuery
        {
            BudgetId             = q.BudgetId,
            Page                 = 1,
            PageSize             = int.MaxValue,
            SortBy               = q.SortBy,
            SortDir              = q.SortDir,
            FilterByStartDate    = q.FilterByStartDate,
            StartDate            = q.StartDate,
            FilterByEndDate      = q.FilterByEndDate,
            EndDate              = q.EndDate,
            FilterByDescription  = q.FilterByDescription,
            Description          = q.Description,
            FilterByAmount       = q.FilterByAmount,
            Amount               = q.Amount,
            FilterByCategory     = q.FilterByCategory,
            CategoryId           = q.CategoryId,
            FilterByRecurrence   = q.FilterByRecurrence,
            Recurrence           = q.Recurrence,
            FilterByMonth        = q.FilterByMonth,
            Month                = q.Month,
            ProjectId            = q.ProjectId,
            Type                 = q.Type,
            IsActive             = q.IsActive
        };
        var (items, _) = await GetPagedAsync(exportQuery);
        return items;
    }
}
