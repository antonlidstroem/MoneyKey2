using MoneyKey.Core.DTOs.TimeEntry;
using MoneyKey.DAL.Data;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Constants;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.Core.Services;

public class TimeTrackingService
{
    private readonly ITimeEntryRepository _entryRepo;
    private readonly IJobRepository       _jobRepo;
    private readonly ITransactionRepository _txRepo;
    private readonly BudgetDbContext _db;

    public TimeTrackingService(
        ITimeEntryRepository entryRepo,
        IJobRepository jobRepo,
        ITransactionRepository txRepo,
        BudgetDbContext db)
    {
        _entryRepo = entryRepo;
        _jobRepo   = jobRepo;
        _txRepo    = txRepo;
        _db        = db;
    }

    // ── Time entries ──────────────────────────────────────────────────────────

    public async Task<TimeEntry> CreateEntryAsync(int budgetId, string userId, CreateTimeEntryDto dto)
    {
        var job = await _jobRepo.GetByIdAsync(dto.JobId, budgetId)
            ?? throw new KeyNotFoundException("Jobb hittades inte.");

        // Compute duration from start/end if not explicitly set
        var duration = dto.DurationMinutes;
        if (duration <= 0 && dto.StartTime.HasValue && dto.EndTime.HasValue)
            duration = (int)(dto.EndTime.Value - dto.StartTime.Value).TotalMinutes;

        var entry = new TimeEntry
        {
            BudgetId          = budgetId,
            UserId            = userId,
            JobId             = dto.JobId,
            Date              = dto.Date,
            StartTime         = dto.StartTime,
            EndTime           = dto.EndTime,
            DurationMinutes   = Math.Max(0, duration),
            Description       = dto.Description,
            IsBreak           = dto.IsBreak,
            HourlyRateOverride = dto.HourlyRateOverride
        };
        return await _entryRepo.CreateAsync(entry);
    }

    // ── Payroll posting ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds a preview of what will be posted.
    /// Returns null if no unposted entries exist for the period.
    /// </summary>
    public async Task<PayrollPeriodSummaryDto?> GetPeriodSummaryAsync(
        int budgetId, int jobId, string periodKey)
    {
        var job     = await _jobRepo.GetByIdAsync(jobId, budgetId);
        if (job == null) return null;

        var entries = await _entryRepo.GetUnpostedAsync(budgetId, jobId, periodKey);
        if (!entries.Any()) return null;

        var rate        = job.HourlyRate ?? 0;
        var totalMins   = entries.Sum(e => e.DurationMinutes);
        var totalHours  = totalMins / 60m;
        var gross       = entries.Sum(e => e.GrossEarned(rate));

        var parts   = periodKey.Split('-');
        var year    = int.Parse(parts[0]);
        var month   = int.Parse(parts[1]);
        var label   = $"{new DateTime(year, month, 1):MMMM yyyy}";

        return new PayrollPeriodSummaryDto
        {
            PeriodKey   = periodKey,
            PeriodLabel = label,
            JobId       = jobId,
            JobName     = job.Name,
            HourlyRate  = rate,
            TotalHours  = totalHours,
            GrossAmount = gross,
            Entries     = entries.Select(e => ToDto(e, rate)).ToList()
        };
    }

    /// <summary>
    /// Creates one Lön transaction and marks all entries as posted.
    /// Guards: entries must be unposted; job must use AutoCreate mode.
    /// </summary>
    public async Task<Transaction> PostToPayrollAsync(
        int budgetId, string userId, PostToPayrollDto dto)
    {
        var job = await _jobRepo.GetByIdAsync(dto.JobId, budgetId)
            ?? throw new KeyNotFoundException("Jobb hittades inte.");

        if (job.TransactionMode == JobTransactionMode.ManualOnly)
            throw new InvalidOperationException(
                "Detta jobb är satt till ManualOnly — transaktioner hanteras manuellt.");

        // Verify all entry IDs belong to this budget and are unposted
        var entries = new List<TimeEntry>();
        foreach (var id in dto.EntryIds)
        {
            var e = await _entryRepo.GetByIdAsync(id, budgetId)
                ?? throw new KeyNotFoundException($"Tidpost {id} hittades inte.");
            if (e.LinkedTransactionId != null)
                throw new InvalidOperationException($"Tidpost {id} är redan postad.");
            entries.Add(e);
        }

        var description = dto.Description
            ?? $"Lön [{job.Name}] {dto.PayrollPeriodKey} — {entries.Sum(e => e.DurationMinutes) / 60m:N1} tim";

        // B4 fix: atomic transaction — both ops succeed or both fail
        await using var dbTx = await _db.Database.BeginTransactionAsync();
        try
        {
            var transaction = new Transaction
            {
                BudgetId        = budgetId,
                StartDate       = DateTime.Today,
                NetAmount       = dto.NetAmount,
                GrossAmount     = dto.GrossAmount,
                Description     = description,
                CategoryId      = CategoryConstants.Salary,
                Type            = TransactionType.Income,
                Recurrence      = Recurrence.OneTime,
                IsActive        = true,
                CreatedByUserId = userId
            };
            transaction = await _txRepo.CreateAsync(transaction);
            await _entryRepo.MarkPostedAsync(dto.EntryIds, transaction.Id, dto.PayrollPeriodKey);
            await dbTx.CommitAsync();
            return transaction;
        }
        catch
        {
            await dbTx.RollbackAsync();
            throw;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public static TimeEntryDto ToDto(TimeEntry e, decimal jobRate) => new()
    {
        Id                  = e.Id,
        BudgetId            = e.BudgetId,
        UserId              = e.UserId,
        JobId               = e.JobId,
        JobName             = e.Job?.Name ?? "",
        JobHourlyRate       = jobRate,
        Date                = e.Date,
        StartTime           = e.StartTime,
        EndTime             = e.EndTime,
        DurationMinutes     = e.DurationMinutes,
        Description         = e.Description,
        IsBreak             = e.IsBreak,
        HourlyRateOverride  = e.HourlyRateOverride,
        EffectiveRate       = e.EffectiveRate(jobRate),
        GrossEarned         = e.GrossEarned(jobRate),
        LinkedTransactionId = e.LinkedTransactionId,
        PayrollPeriodKey    = e.PayrollPeriodKey,
        CreatedAt           = e.CreatedAt
    };

    public static string GetPeriodKey(DateTime date) => date.ToString("yyyy-MM");
    public static string GetPeriodLabel(string key)
    {
        var parts = key.Split('-');
        return $"{new DateTime(int.Parse(parts[0]), int.Parse(parts[1]), 1):MMMM yyyy}";
    }
}
