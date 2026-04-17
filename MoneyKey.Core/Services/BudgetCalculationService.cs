using MoneyKey.Core.DTOs.Journal;
using MoneyKey.Core.DTOs.Summary;
using MoneyKey.Domain.Enums;

namespace MoneyKey.Core.Services;

public class BudgetCalculationService
{
    public SummaryDto ComputeSummary(List<JournalEntryDto> entries)
    {
        // VAB entries: TransactionType.Income with negative amount = lost income.
        // They must NOT count as expenses. Separate them out first.
        var vabEntries = entries.Where(e => e.EntryType == JournalEntryType.Vab).ToList();

        // Receipt batches: only count Approved/Reimbursed as actual expenses
        var countable = entries.Where(e =>
            e.EntryType != JournalEntryType.ReceiptBatch ||
            e.Status is "Godkänd" or "Utbetald").ToList();

        // Milersättning entries (income)
        var milersattning = countable
            .Where(e => e.EntryType == JournalEntryType.Milersattning)
            .Sum(e => e.Amount);

        // VAB lost income = the absolute reduction to income from VAB absences
        var vabLost = vabEntries.Sum(e => Math.Abs(e.Amount));

        // Regular transactions only (for monthly forecast, exclude VAB and Milersättning)
        var txOnly = countable.Where(e => e.EntryType == JournalEntryType.Transaction).ToList();

        // Total income: sum of all positive amounts (excluding VAB negatives, including milersättning)
        // VAB creates a negative-income transaction — we exclude those from FilteredIncome
        // because we report them separately as VabLostIncome.
        var filteredIncome   = countable
            .Where(e => e.Amount > 0 && e.EntryType != JournalEntryType.Vab)
            .Sum(e => e.Amount);

        var filteredExpenses = countable
            .Where(e => e.Amount < 0 && e.EntryType != JournalEntryType.Vab)
            .Sum(e => e.Amount);

        return new SummaryDto
        {
            FilteredIncome     = filteredIncome,
            FilteredExpenses   = filteredExpenses,
            MonthlyIncome      = txOnly.Where(e => e.Amount > 0).Sum(e => e.Amount),
            MonthlyExpenses    = txOnly.Where(e => e.Amount < 0).Sum(e => e.Amount),
            MilersattningIncome = milersattning,
            VabLostIncome      = vabLost
        };
    }
}
