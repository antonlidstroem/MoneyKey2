using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories.Interfaces;

public interface IKonteringRepository
{
    Task<List<KonteringRow>> GetForTransactionAsync(int transactionId);
    Task                     SaveRowsAsync(int transactionId, List<KonteringRow> rows);
}
