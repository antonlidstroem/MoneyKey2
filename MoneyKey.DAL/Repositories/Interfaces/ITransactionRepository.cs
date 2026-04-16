using MoneyKey.DAL.Queries;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories.Interfaces;

public interface ITransactionRepository
{
    Task<(List<Transaction> Items, int TotalCount)> GetPagedAsync(TransactionQuery query);
    Task<Transaction?> GetByIdAsync(int id, int budgetId);
    Task<Transaction>  CreateAsync(Transaction transaction);
    Task<Transaction>  UpdateAsync(Transaction transaction);
    Task DeleteAsync(int id, int budgetId);
    Task DeleteBatchAsync(List<int> ids, int budgetId);
    Task<List<Transaction>> GetForExportAsync(TransactionQuery query);
}
