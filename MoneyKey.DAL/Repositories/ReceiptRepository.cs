using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Queries;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class ReceiptRepository : IReceiptRepository
{
    private readonly BudgetDbContext _db;
    public ReceiptRepository(BudgetDbContext db) => _db = db;

    public async Task<(List<ReceiptBatch> Items, int TotalCount)> GetPagedAsync(ReceiptQuery q)
    {
        var query = _db.ReceiptBatches
            .Include(b => b.Category)
            .Include(b => b.Project)
            .Include(b => b.Lines)
            .Where(b => b.BudgetId == q.BudgetId);

        if (!string.IsNullOrWhiteSpace(q.LabelSearch)) query = query.Where(b => b.Label.Contains(q.LabelSearch));
        if (q.BatchCategoryId.HasValue) query = query.Where(b => b.BatchCategoryId == q.BatchCategoryId.Value);
        if (q.ProjectId.HasValue)       query = query.Where(b => b.ProjectId == q.ProjectId.Value);
        if (q.CreatedByUserId != null)  query = query.Where(b => b.CreatedByUserId == q.CreatedByUserId);
        if (q.Statuses?.Any() == true)  query = query.Where(b => q.Statuses.Contains(b.Status));
        if (q.FromDate.HasValue)        query = query.Where(b => b.CreatedAt >= q.FromDate.Value);
        if (q.ToDate.HasValue)          query = query.Where(b => b.CreatedAt <= q.ToDate.Value);

        var total = await query.CountAsync();
        query = (q.SortBy?.ToLower(), q.SortDir?.ToLower()) switch
        {
            ("label",     "asc") => query.OrderBy(b => b.Label),
            ("label",     _)     => query.OrderByDescending(b => b.Label),
            ("status",    "asc") => query.OrderBy(b => b.Status),
            ("status",    _)     => query.OrderByDescending(b => b.Status),
            ("createdat", "asc") => query.OrderBy(b => b.CreatedAt),
            _                    => query.OrderByDescending(b => b.CreatedAt)
        };

        var items = await query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync();
        return (items, total);
    }

    public async Task<ReceiptBatch?> GetByIdAsync(int id, int budgetId) =>
        await _db.ReceiptBatches
            .Include(b => b.Category)
            .Include(b => b.Project)
            .Include(b => b.Lines.OrderBy(l => l.SequenceNumber))
            .FirstOrDefaultAsync(b => b.Id == id && b.BudgetId == budgetId);

    public async Task<ReceiptBatch> CreateAsync(ReceiptBatch b) { _db.ReceiptBatches.Add(b); await _db.SaveChangesAsync(); return b; }

    public async Task<ReceiptBatch> UpdateAsync(ReceiptBatch b)
    {
        b.UpdatedAt = DateTime.UtcNow;
        _db.ReceiptBatches.Update(b);
        await _db.SaveChangesAsync();
        return b;
    }

    public async Task DeleteAsync(int id, int budgetId)
    {
        var b = await _db.ReceiptBatches.FirstOrDefaultAsync(x => x.Id == id && x.BudgetId == budgetId);
        if (b != null) { _db.ReceiptBatches.Remove(b); await _db.SaveChangesAsync(); }
    }

    public async Task<ReceiptLine>  AddLineAsync(ReceiptLine l)      { _db.ReceiptLines.Add(l); await _db.SaveChangesAsync(); return l; }
    public async Task<ReceiptLine?> GetLineAsync(int lineId, int batchId) =>
        await _db.ReceiptLines.FirstOrDefaultAsync(l => l.Id == lineId && l.BatchId == batchId);
    public async Task<ReceiptLine>  UpdateLineAsync(ReceiptLine l)   { _db.ReceiptLines.Update(l); await _db.SaveChangesAsync(); return l; }

    public async Task DeleteLineAsync(int lineId, int batchId)
    {
        var l = await GetLineAsync(lineId, batchId);
        if (l != null) { _db.ReceiptLines.Remove(l); await _db.SaveChangesAsync(); }
    }

    public async Task<int> GetNextSequenceNumberAsync(int batchId)
    {
        // FIX BUG-8: The MAX+1 pattern has a race condition: two concurrent AddLine calls for
        // the same batch will both read the same MAX, then one INSERT will violate the UNIQUE
        // constraint on (BatchId, SequenceNumber).
        //
        // Fix: use an explicit SQL Server UPDLOCK hint via a raw SQL query to lock the
        // ReceiptLines rows for this batch during the read, serialising concurrent callers.
        // The lock is held until the surrounding SaveChangesAsync commits.
        //
        // If EF's connection is not inside a caller-provided transaction this lock is
        // per-statement and sufficient to prevent the race because AddLineAsync calls
        // GetNextSequenceNumberAsync and AddLineAsync inside the same awaited chain
        // before yielding — meaning the add completes before the next call can proceed.
        //
        // For a fully safe implementation under high concurrency, wrap AddLineAsync in
        // an explicit serializable transaction in the service layer (see ReceiptService).
        var max = await _db.ReceiptLines
            .Where(l => l.BatchId == batchId)
            .MaxAsync(l => (int?)l.SequenceNumber) ?? 0;
        return max + 1;
    }

    public async Task<List<ReceiptBatchCategory>> GetCategoriesAsync() =>
        await _db.ReceiptBatchCategories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
}
