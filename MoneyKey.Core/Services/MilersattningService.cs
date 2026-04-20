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

    /// <summary>
    /// Skatteverket standard rate 2024 = 0.25 kr/km (25 öre/km) for private car.
    /// Tax-free limit is 2.50 kr/km. Values above 0.25 are employer supplements.
    /// </summary>
    public const decimal SkatteverketStandardRate = 0.25m;

    public MilersattningService(IMilersattningRepository repo, ITransactionRepository txRepo, IAppSettingRepository settings)
    { _repo = repo; _txRepo = txRepo; _settings = settings; }

    public async Task<MilersattningEntry> CreateAsync(int budgetId, string userId, CreateMilersattningDto dto)
    {
        var rate = dto.RatePerKm > 0 ? dto.RatePerKm : await GetRateAsync(budgetId);

        var entry = new MilersattningEntry
        {
            BudgetId     = budgetId,
            UserId       = userId,
            TripDate     = dto.TripDate,
            FromLocation = dto.FromLocation,
            ToLocation   = dto.ToLocation,
            DistanceKm   = dto.DistanceKm,
            IsRoundTrip  = dto.IsRoundTrip,
            RatePerKm    = rate,
            Purpose      = dto.Purpose,
            PayerName    = dto.PayerName,
            Status       = MilersattningStatus.Draft
        };
        entry = await _repo.CreateAsync(entry);

        // ReimbursementAmount already uses EffectiveDistanceKm (accounts for round-trip)
        var routeDescription = dto.IsRoundTrip
            ? $"{dto.FromLocation} → {dto.ToLocation} (tur-retur, {dto.DistanceKm:N0} km × 2 = {entry.EffectiveDistanceKm:N0} km)"
            : $"{dto.FromLocation} → {dto.ToLocation} ({dto.DistanceKm:N0} km)";

        var tx = new Transaction
        {
            BudgetId             = budgetId,
            StartDate            = dto.TripDate,
            NetAmount            = entry.ReimbursementAmount,
            Description          = $"Milersättning: {routeDescription}",
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

    public async Task<MilersattningEntry?> UpdateStatusAsync(int id, int budgetId, MilersattningStatus newStatus)
    {
        var entry = await _repo.GetByIdAsync(id, budgetId);
        if (entry == null) return null;

        entry.Status = newStatus;
        entry.SubmittedAt = newStatus >= MilersattningStatus.Submitted ? entry.SubmittedAt ?? DateTime.UtcNow : null;
        entry.ApprovedAt  = newStatus >= MilersattningStatus.Approved  ? entry.ApprovedAt  ?? DateTime.UtcNow : null;
        entry.PaidAt      = newStatus == MilersattningStatus.Paid       ? entry.PaidAt      ?? DateTime.UtcNow : null;

        return await _repo.UpdateAsync(entry);
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
        return decimal.TryParse(stored, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var r)
            ? r : SkatteverketStandardRate;
    }

    public static string SwedishStatus(MilersattningStatus s) => s switch
    {
        MilersattningStatus.Draft     => "Utkast",
        MilersattningStatus.Submitted => "Inskickad",
        MilersattningStatus.Approved  => "Godkänd",
        MilersattningStatus.Paid      => "Utbetald",
        _                             => s.ToString()
    };

    public static MilersattningDto ToDto(MilersattningEntry m, string statusLabel) => new()
    {
        Id = m.Id, BudgetId = m.BudgetId, UserId = m.UserId,
        TripDate = m.TripDate, FromLocation = m.FromLocation, ToLocation = m.ToLocation,
        DistanceKm = m.DistanceKm, IsRoundTrip = m.IsRoundTrip,
        EffectiveDistanceKm = m.EffectiveDistanceKm,
        RatePerKm = m.RatePerKm, Purpose = m.Purpose, PayerName = m.PayerName,
        Status = m.Status, StatusLabel = statusLabel,
        ReimbursementAmount = m.ReimbursementAmount,
        LinkedTransactionId = m.LinkedTransactionId
    };
}
