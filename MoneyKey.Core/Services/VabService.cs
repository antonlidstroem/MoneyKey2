using MoneyKey.Core.DTOs.Vab;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Constants;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.Core.Services;

public class VabService
{
    private readonly IVabRepository         _repo;
    private readonly ITransactionRepository _txRepo;

    public VabService(IVabRepository repo, ITransactionRepository txRepo)
    { _repo = repo; _txRepo = txRepo; }

    public async Task<VabEntry> CreateAsync(int budgetId, string userId, CreateVabDto dto)
    {
        var entry = new VabEntry
        {
            BudgetId     = budgetId,
            UserId       = userId,
            ChildName    = dto.ChildName,
            StartDate    = dto.StartDate,
            EndDate      = dto.EndDate,
            DailyBenefit = dto.DailyBenefit,
            Rate         = dto.Rate
        };
        entry = await _repo.CreateAsync(entry);

        // VAB = "Utebliven inkomst" — lost income, NOT an expense.
        // Stored as a NEGATIVE Income transaction so it reduces net income correctly
        // without inflating the expense total. The financial advisor clarification:
        // VAB compensation reduces what you WOULD have earned; it is not money you spend.
        var description = string.IsNullOrWhiteSpace(dto.ChildName)
            ? $"VAB {dto.StartDate:d}–{dto.EndDate:d} ({entry.TotalDays} dagar, {dto.Rate * 100:N0}%)"
            : $"VAB {dto.ChildName}: {dto.StartDate:d}–{dto.EndDate:d} ({entry.TotalDays} dagar, {dto.Rate * 100:N0}%)";

        var tx = new Transaction
        {
            BudgetId        = budgetId,
            StartDate       = dto.StartDate,
            EndDate         = dto.EndDate,
            // Negative income = lost income. Not an expense.
            NetAmount       = -entry.TotalAmount,
            Description     = description,
            CategoryId      = CategoryConstants.VabSjukfranvaro,
            // Corrected: TransactionType.Income with negative amount = lost income.
            // This way VAB is excluded from FilteredExpenses in summaries and shown separately.
            Type            = TransactionType.Income,
            Recurrence      = Recurrence.OneTime,
            IsActive        = true,
            CreatedByUserId = userId,
            VabEntryId      = entry.Id
        };
        tx = await _txRepo.CreateAsync(tx);
        entry.LinkedTransactionId = tx.Id;
        await _repo.UpdateAsync(entry);
        return entry;
    }

    public async Task DeleteAsync(int id, int budgetId)
    {
        var entry = await _repo.GetByIdAsync(id, budgetId);
        if (entry?.LinkedTransactionId != null)
            await _txRepo.DeleteAsync(entry.LinkedTransactionId.Value, budgetId);
        await _repo.DeleteAsync(id, budgetId);
    }

    public VabDto ToDto(VabEntry v) => new VabDto
    {
        Id = v.Id,
        BudgetId = v.BudgetId,
        UserId = v.UserId,
        ChildName = v.ChildName,
        StartDate = v.StartDate,
        EndDate = v.EndDate,
        DailyBenefit = v.DailyBenefit,
        Rate = v.Rate,
        TotalDays = v.TotalDays,
        TotalAmount = v.TotalAmount,
        LinkedTransactionId = v.LinkedTransactionId
    };

    /// <summary>
    /// Calculates the actual SGI-based daily VAB benefit.
    /// SGI = Sjukpenninggrundande inkomst (qualifying annual income).
    /// Daily VAB = SGI / 365 * 0.8 (80% rule from Försäkringskassan).
    /// First day is always unpaid (karensdag).
    /// </summary>
    public static decimal CalculateDailyBenefit(decimal annualSgi)
        => Math.Round(annualSgi / 365m * 0.8m, 2);

    // inline i EntryFormModal.razor
    private static decimal CalcVabDaily(decimal sgi) => Math.Round(sgi / 365m * 0.97m, 2);
}
