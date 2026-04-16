using MoneyKey.Core.DTOs.Journal;
using MoneyKey.Core.DTOs.Summary;
using MoneyKey.Domain.Enums;

namespace MoneyKey.Core.Services;

public class BudgetCalculationService
{
    public SummaryDto ComputeSummary(List<JournalEntryDto> entries)
    {
        var countable = entries.Where(e =>
            e.EntryType != JournalEntryType.ReceiptBatch ||
            e.Status is "Godkänd" or "Utbetald").ToList();

        return new SummaryDto
        {
            FilteredIncome   = countable.Where(e => e.Amount > 0).Sum(e => e.Amount),
            FilteredExpenses = countable.Where(e => e.Amount < 0).Sum(e => e.Amount),
            MonthlyIncome    = countable.Where(e => e.EntryType == JournalEntryType.Transaction && e.Amount > 0).Sum(e => e.Amount),
            MonthlyExpenses  = countable.Where(e => e.EntryType == JournalEntryType.Transaction && e.Amount < 0).Sum(e => e.Amount)
        };
    }
}
