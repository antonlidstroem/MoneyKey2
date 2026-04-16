using MoneyKey.DAL.Queries;
using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories.Interfaces;

public interface IReceiptRepository
{
    Task<(List<ReceiptBatch> Items, int TotalCount)> GetPagedAsync(ReceiptQuery query);
    Task<ReceiptBatch?>  GetByIdAsync(int id, int budgetId);
    Task<ReceiptBatch>   CreateAsync(ReceiptBatch batch);
    Task<ReceiptBatch>   UpdateAsync(ReceiptBatch batch);
    Task                 DeleteAsync(int id, int budgetId);
    Task<ReceiptLine>    AddLineAsync(ReceiptLine line);
    Task<ReceiptLine?>   GetLineAsync(int lineId, int batchId);
    Task<ReceiptLine>    UpdateLineAsync(ReceiptLine line);
    Task                 DeleteLineAsync(int lineId, int batchId);
    Task<int>            GetNextSequenceNumberAsync(int batchId);
    Task<List<ReceiptBatchCategory>> GetCategoriesAsync();
}
