using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MoneyKey.DAL.Data.Interfaces;
using MoneyKey.Domain.Enums;
using DomainModels = MoneyKey.Domain.Models;

namespace MoneyKey.DAL.Data;

/// <summary>
/// Automatically captures all CRUD changes and writes AuditLog rows.
/// KonteringRow resolves BudgetId via its parent Transaction from the change tracker.
///
/// FIX BUG-9: New Budget inserts are logged with BudgetId=0 during SavingChanges because
/// the SQL Server IDENTITY value is not yet assigned. We solve this by storing a pending
/// AuditLog per Budget-Add and updating its BudgetId in SavedChangesAsync after the INSERT
/// has completed and EF has refreshed the generated keys.
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserAccessor _user;

    // Tracks AuditLog entries whose BudgetId must be back-filled after SaveChanges commits.
    private readonly List<(DomainModels.AuditLog Log, DomainModels.Budget Budget)> _pendingBudgetLogs = new();

    public AuditInterceptor(ICurrentUserAccessor user) => _user = user;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData data, InterceptionResult<int> result)
    { AddAuditEntries(data.Context); return base.SavingChanges(data, result); }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData data, InterceptionResult<int> result, CancellationToken ct = default)
    { AddAuditEntries(data.Context); return base.SavingChangesAsync(data, result, ct); }

    // After SaveChanges EF refreshes generated keys — now we can back-fill BudgetId.
    public override int SavedChanges(SaveChangesCompletedEventData data, int result)
    { BackFillBudgetIds(); return base.SavedChanges(data, result); }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData data, int result, CancellationToken ct = default)
    { BackFillBudgetIds(); return base.SavedChangesAsync(data, result, ct); }

    private void BackFillBudgetIds()
    {
        foreach (var (log, budget) in _pendingBudgetLogs)
            log.BudgetId = budget.Id;  // Id is now the real IDENTITY value
        _pendingBudgetLogs.Clear();
    }

    private void AddAuditEntries(DbContext? ctx)
    {
        if (ctx == null) return;

        var entries = ctx.ChangeTracker.Entries().ToList();

        foreach (var entry in entries)
        {
            if (!IsAuditable(entry.Entity)) continue;
            if (entry.State is EntityState.Unchanged or EntityState.Detached) continue;

            var action = entry.State switch
            {
                EntityState.Added    => AuditAction.Created,
                EntityState.Modified => AuditAction.Updated,
                EntityState.Deleted  => AuditAction.Deleted,
                _                    => AuditAction.Updated
            };

            var budgetId = GetBudgetId(entry, entries);

            if (budgetId == 0 && entry.Entity is not DomainModels.Budget) continue;

            string? oldVals = null, newVals = null;

            if (entry.State == EntityState.Modified)
            {
                var modProps = entry.Properties.Where(p => p.IsModified)
                                   .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
                var curProps = entry.Properties.Where(p => p.IsModified)
                                   .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
                oldVals = JsonSerializer.Serialize(modProps);
                newVals = JsonSerializer.Serialize(curProps);
            }
            else if (entry.State == EntityState.Added)
            {
                newVals = JsonSerializer.Serialize(
                    entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
            }
            else
            {
                oldVals = JsonSerializer.Serialize(
                    entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
            }

            var key = entry.Metadata.FindPrimaryKey()?.Properties
                           .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
                           .FirstOrDefault() ?? "0";

            var auditLog = new DomainModels.AuditLog
            {
                BudgetId   = budgetId,   // May be 0 for new Budget — back-filled in SavedChanges
                UserId     = _user.UserId,
                UserEmail  = _user.UserEmail,
                EntityName = entry.Entity.GetType().Name,
                EntityId   = key,
                Action     = action,
                OldValues  = oldVals,
                NewValues  = newVals,
                Timestamp  = DateTime.UtcNow
            };
            ctx.Set<DomainModels.AuditLog>().Add(auditLog);

            // FIX BUG-9: For newly-added Budget entities, BudgetId is still 0 here
            // because the IDENTITY value hasn't been assigned yet.
            // Register this log for back-fill after SaveChanges returns.
            if (entry.Entity is DomainModels.Budget b && entry.State == EntityState.Added)
                _pendingBudgetLogs.Add((auditLog, b));
        }
    }

    private static bool IsAuditable(object e) => e is
        DomainModels.Transaction       or DomainModels.Project          or
        DomainModels.KonteringRow      or DomainModels.MilersattningEntry or
        DomainModels.VabEntry          or DomainModels.ReceiptBatch     or
        DomainModels.ReceiptLine       or DomainModels.Budget           or
        DomainModels.BudgetMembership;

    private static int GetBudgetId(EntityEntry entry, IReadOnlyList<EntityEntry> allEntries) =>
        entry.Entity switch
        {
            DomainModels.Transaction t        => t.BudgetId,
            DomainModels.Project p            => p.BudgetId,
            DomainModels.MilersattningEntry m  => m.BudgetId,
            DomainModels.VabEntry v           => v.BudgetId,
            DomainModels.ReceiptBatch rb      => rb.BudgetId,
            DomainModels.Budget b             => b.Id,       // 0 for new inserts — back-filled later
            DomainModels.BudgetMembership bm  => bm.BudgetId,
            DomainModels.KonteringRow k       => ResolveKonteringBudgetId(k, allEntries),
            _ => 0
        };

    private static int ResolveKonteringBudgetId(
        DomainModels.KonteringRow row,
        IReadOnlyList<EntityEntry> allEntries)
    {
        var parentEntry = allEntries.FirstOrDefault(e =>
            e.Entity is DomainModels.Transaction t && t.Id == row.TransactionId);

        if (parentEntry?.Entity is DomainModels.Transaction tx)
            return tx.BudgetId;

        if (row.Transaction != null)
            return row.Transaction.BudgetId;

        return 0;
    }
}
