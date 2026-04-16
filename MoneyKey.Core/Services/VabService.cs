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
    {
        _repo   = repo;
        _txRepo = txRepo;
    }

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

        var tx = new Transaction
        {
            BudgetId        = budgetId,
            StartDate       = dto.StartDate,
            EndDate         = dto.EndDate,
            NetAmount       = -entry.TotalAmount,
            Description     = string.IsNullOrWhiteSpace(dto.ChildName)
                                ? $"VAB {dto.StartDate:d}–{dto.EndDate:d} ({entry.TotalDays} dagar)"
                                : $"VAB {dto.ChildName}: {dto.StartDate:d}–{dto.EndDate:d} ({entry.TotalDays} dagar)",
            CategoryId      = CategoryConstants.VabSjukfranvaro,
            Type            = TransactionType.Expense,
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
}
