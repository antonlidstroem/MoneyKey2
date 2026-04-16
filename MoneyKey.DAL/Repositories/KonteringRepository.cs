using Microsoft.EntityFrameworkCore;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories;

public class KonteringRepository : IKonteringRepository
{
    private readonly BudgetDbContext _db;
    public KonteringRepository(BudgetDbContext db) => _db = db;

    public async Task<List<KonteringRow>> GetForTransactionAsync(int transactionId) =>
        await _db.KonteringRows.Where(k => k.TransactionId == transactionId).ToListAsync();

    public async Task SaveRowsAsync(int transactionId, List<KonteringRow> rows)
    {
        var existing = await _db.KonteringRows.Where(k => k.TransactionId == transactionId).ToListAsync();
        _db.KonteringRows.RemoveRange(existing);
        foreach (var r in rows) r.TransactionId = transactionId;
        _db.KonteringRows.AddRange(rows);
        await _db.SaveChangesAsync();
    }
}
