using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories.Interfaces;

public interface IAuditRepository
{
    Task LogAsync(AuditLog entry);
    Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(int budgetId, int page, int pageSize);
}
