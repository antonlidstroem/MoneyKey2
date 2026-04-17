namespace MoneyKey.Core.DTOs.Summary;

public class SummaryDto
{
    // ── Filtered view (all types in current filter) ──────────────────────────
    public decimal FilteredIncome   { get; set; }
    public decimal FilteredExpenses { get; set; }
    public decimal FilteredTotal    => FilteredIncome + FilteredExpenses;

    // ── Transaction-only (for recurring/monthly forecasting) ─────────────────
    public decimal MonthlyIncome    { get; set; }
    public decimal MonthlyExpenses  { get; set; }
    public decimal MonthlyTotal     => MonthlyIncome + MonthlyExpenses;

    // ── Milersättning breakdown ───────────────────────────────────────────────
    public decimal MilersattningIncome { get; set; }

    // ── VAB: "Utebliven inkomst" — not counted as expense in totals ──────────
    /// <summary>
    /// Total VAB compensation amount. This represents lost income, NOT an expense.
    /// Shown as a separate line in summaries; excluded from FilteredExpenses.
    /// </summary>
    public decimal VabLostIncome    { get; set; }

    // ── Financial ratios ──────────────────────────────────────────────────────
    /// <summary>
    /// Savings rate = (FilteredIncome - |FilteredExpenses|) / FilteredIncome.
    /// Displayed as a percentage; negative means deficit.
    /// </summary>
    public decimal SavingsRate =>
        FilteredIncome > 0
            ? Math.Round((FilteredIncome + FilteredExpenses) / FilteredIncome * 100, 1)
            : 0;
}
