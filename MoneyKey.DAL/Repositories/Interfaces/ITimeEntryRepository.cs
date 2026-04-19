using MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Repositories.Interfaces;

public interface ITimeEntryRepository
{
    Task<List<TimeEntry>> GetForBudgetAsync(int budgetId, int? jobId = null, DateTime? from = null, DateTime? to = null);
    Task<List<TimeEntry>> GetUnpostedAsync(int budgetId, int jobId, string periodKey);
    Task<TimeEntry?>      GetByIdAsync(int id, int budgetId);
    Task<TimeEntry>       CreateAsync(TimeEntry entry);
    Task<TimeEntry>       UpdateAsync(TimeEntry entry);
    Task                  DeleteAsync(int id, int budgetId);
    Task                  MarkPostedAsync(List<int> entryIds, int transactionId, string periodKey);
}
