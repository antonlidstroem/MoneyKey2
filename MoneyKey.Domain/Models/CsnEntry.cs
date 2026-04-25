namespace MoneyKey.Domain.Models;

/// <summary>
/// Tracks CSN debt, repayment schedule and annual income limit (fribeloppsgräns).
/// Intended pattern: one entry per year per user.
/// </summary>
public class CsnEntry
{
    public int Id { get; set; }
    public int BudgetId { get; set; }
    public Budget Budget { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public int Year { get; set; } = DateTime.Today.Year;

    // ── Skuld ─────────────────────────────────────────────────────────────────
    public decimal TotalOriginalDebt { get; set; }  // Ursprunglig skuld
    public decimal CurrentBalance { get; set; }  // Nuvarande skuld
    public decimal AnnualRepayment { get; set; }  // Årsbelopp

    // ── Fribeloppsgräns ───────────────────────────────────────────────────────
    public decimal AnnualIncomeLimit { get; set; }
    public decimal EstimatedAnnualIncome { get; set; }

    // ── Studiegång (valfritt) ─────────────────────────────────────────────────
    public bool IsCurrentlyStudying { get; set; }
    public decimal? MonthlyStudyGrant { get; set; }  // Studiebidrag/mån
    public decimal? MonthlyStudyLoan { get; set; }  // Studielånsuttag/mån

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Beräknade egenskaper (ignoreras av EF) ────────────────────────────────
    public decimal MonthlyRepayment => AnnualRepayment > 0 ? AnnualRepayment / 12m : 0;
    public decimal IncomeMargin => AnnualIncomeLimit - EstimatedAnnualIncome;
    public decimal YearsRemaining => AnnualRepayment > 0
                                         ? Math.Round(CurrentBalance / AnnualRepayment, 1) : 0;
    public bool IsOverIncomeLimit => EstimatedAnnualIncome > AnnualIncomeLimit && AnnualIncomeLimit > 0;
}