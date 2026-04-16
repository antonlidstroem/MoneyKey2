using MoneyKey.Core.DTOs.Milersattning;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Constants;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.Core.Services;

public class MilersattningService
{
    private readonly IMilersattningRepository _repo;
    private readonly ITransactionRepository   _txRepo;
    private readonly IAppSettingRepository    _settings;

    public MilersattningService(
        IMilersattningRepository repo,
        ITransactionRepository txRepo,
        IAppSettingRepository settings)
    {
        _repo     = repo;
        _txRepo   = txRepo;
        _settings = settings;
    }

    public async Task<MilersattningEntry> CreateAsync(int budgetId, string userId, CreateMilersattningDto dto)
    {
        var rate  = await GetRateAsync(budgetId);
        var entry = new MilersattningEntry
        {
            BudgetId     = budgetId,
            UserId       = userId,
            TripDate     = dto.TripDate,
            FromLocation = dto.FromLocation,
            ToLocation   = dto.ToLocation,
            DistanceKm   = dto.DistanceKm,
            RatePerKm    = dto.RatePerKm > 0 ? dto.RatePerKm : rate,
            Purpose      = dto.Purpose
        };
        entry = await _repo.CreateAsync(entry);

        var tx = new Transaction
        {
            BudgetId             = budgetId,
            StartDate            = dto.TripDate,
            NetAmount            = entry.ReimbursementAmount,
            Description          = $"Milersättning: {dto.FromLocation} → {dto.ToLocation} ({dto.DistanceKm} km)",
            CategoryId           = CategoryConstants.Milersattning,
            Type                 = TransactionType.Income,
            Recurrence           = Recurrence.OneTime,
            IsActive             = true,
            CreatedByUserId      = userId,
            MilersattningEntryId = entry.Id
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

    public async Task<decimal> GetRateAsync(int budgetId)
    {
        var stored = await _settings.GetAsync(budgetId, "MilersattningRate");
        return decimal.TryParse(stored, out var r) ? r : 0.25m;
    }
}
