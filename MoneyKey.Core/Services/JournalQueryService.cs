using MoneyKey.Core.DTOs.Journal;
using MoneyKey.Core.DTOs.Kontering;
using MoneyKey.Core.DTOs.Lists;
using MoneyKey.Core.DTOs.Summary;
using MoneyKey.DAL.Queries;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;

namespace MoneyKey.Core.Services;

public class JournalQueryService
{
    private readonly ITransactionRepository   _txRepo;
    private readonly IMilersattningRepository _miRepo;
    private readonly IVabRepository           _vabRepo;
    private readonly IReceiptRepository       _receiptRepo;
    private readonly IListRepository          _listRepo;
    private readonly BudgetCalculationService _calculator;

    public JournalQueryService(
        ITransactionRepository txRepo,
        IMilersattningRepository miRepo,
        IVabRepository vabRepo,
        IReceiptRepository receiptRepo,
        IListRepository listRepo,
        BudgetCalculationService calculator)
    {
        _txRepo      = txRepo;
        _miRepo      = miRepo;
        _vabRepo     = vabRepo;
        _receiptRepo = receiptRepo;
        _listRepo    = listRepo;
        _calculator  = calculator;
    }

    public async Task<(List<JournalEntryDto> Items, int TotalCount, SummaryDto Summary)> QueryAsync(JournalQuery q)
    {
        var include = q.IncludeTypes.Count > 0
            ? q.IncludeTypes.ToHashSet()
            : new HashSet<JournalEntryType>(Enum.GetValues<JournalEntryType>());

        var all = new List<JournalEntryDto>();

        if (include.Contains(JournalEntryType.Transaction))
            all.AddRange(await FetchTransactionsAsync(q));

        if (include.Contains(JournalEntryType.Milersattning))
            all.AddRange(await FetchMilersattningAsync(q));

        if (include.Contains(JournalEntryType.Vab))
            all.AddRange(await FetchVabAsync(q));

        if (include.Contains(JournalEntryType.ReceiptBatch))
            all.AddRange(await FetchReceiptsAsync(q));

        // Lists/Notes: fetched separately, not included in summary calculations
        if (include.Contains(JournalEntryType.List))
            all.AddRange(await FetchListsAsync(q));

        all = ApplySharedFilters(all, q);

        all = (q.SortBy?.ToLower(), q.SortDir?.ToLower()) switch
        {
            ("date",        "asc") => all.OrderBy(e => e.Date).ToList(),
            ("date",        _)     => all.OrderByDescending(e => e.Date).ToList(),
            ("amount",      "asc") => all.OrderBy(e => e.Amount).ToList(),
            ("amount",      _)     => all.OrderByDescending(e => e.Amount).ToList(),
            ("description", "asc") => all.OrderBy(e => e.Description).ToList(),
            ("description", _)     => all.OrderByDescending(e => e.Description).ToList(),
            ("type",        "asc") => all.OrderBy(e => e.TypeLabel).ThenByDescending(e => e.Date).ToList(),
            ("type",        _)     => all.OrderByDescending(e => e.TypeLabel).ThenByDescending(e => e.Date).ToList(),
            _ => all.OrderByDescending(e => e.Date).ToList()
        };

        var total   = all.Count;
        // Summary only on non-List entries (lists have no financial impact)
        var summary = _calculator.ComputeSummary(all.Where(e => e.EntryType != JournalEntryType.List).ToList());
        var paged   = all.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToList();

        return (paged, total, summary);
    }

    private async Task<List<JournalEntryDto>> FetchTransactionsAsync(JournalQuery q)
    {
        var tq = new TransactionQuery
        {
            BudgetId          = q.BudgetId,
            Page              = 1,
            PageSize          = int.MaxValue,
            FilterByCategory  = q.FilterByCategory,
            CategoryId        = q.CategoryId,
            ProjectId         = q.FilterByProject ? q.ProjectId : null,
            FilterByStartDate = q.FilterByStartDate,
            StartDate         = q.StartDate,
            FilterByEndDate   = q.FilterByEndDate,
            EndDate           = q.EndDate
        };
        var (txs, _) = await _txRepo.GetPagedAsync(tq);
        return txs.Select(t => new JournalEntryDto
        {
            EntryType      = JournalEntryType.Transaction,
            TypeLabel      = "Transaktion",
            TypeCode       = "T",
            Date           = t.StartDate,
            EndDate        = t.EndDate,
            Amount         = t.NetAmount,
            Description    = t.Description,
            CategoryName   = t.Category?.Name,
            ProjectName    = t.Project?.Name,
            SourceId       = t.Id,
            HasDetail      = t.HasKontering,
            CreatedByEmail = t.CreatedByUserId,
            KonteringRows  = t.KonteringRows.Select(k => new KonteringRowDto
            {
                Id = k.Id, KontoNr = k.KontoNr, CostCenter = k.CostCenter,
                Amount = k.Amount, Percentage = k.Percentage, Description = k.Description
            }).ToList()
        }).ToList();
    }

    private async Task<List<JournalEntryDto>> FetchMilersattningAsync(JournalQuery q)
    {
        var items = await _miRepo.GetForBudgetAsync(q.BudgetId, q.FilterByCreatedBy ? q.CreatedByUserId : null);
        return items.Select(m => new JournalEntryDto
        {
            EntryType      = JournalEntryType.Milersattning,
            TypeLabel      = "Milersättning",
            TypeCode       = "M",
            Date           = m.TripDate,
            Amount         = m.ReimbursementAmount,
            Description    = m.IsRoundTrip
                               ? $"{m.FromLocation} → {m.ToLocation} (tur-retur)"
                               : $"{m.FromLocation} → {m.ToLocation}",
            MetaLine       = $"{m.EffectiveDistanceKm:N0} km · {m.RatePerKm:N2} kr/km"
                           + (m.PayerName != null ? $" · {m.PayerName}" : "")
                           + $" · {SwedishMilStatus(m.Status)}",
            Status         = SwedishMilStatus(m.Status),
            SourceId       = m.Id,
            HasDetail      = false,
            CreatedByEmail = m.UserId
        }).ToList();
    }

    private async Task<List<JournalEntryDto>> FetchVabAsync(JournalQuery q)
    {
        var items = await _vabRepo.GetForBudgetAsync(q.BudgetId, q.FilterByCreatedBy ? q.CreatedByUserId : null);
        return items.Select(v => new JournalEntryDto
        {
            EntryType      = JournalEntryType.Vab,
            TypeLabel      = "VAB",
            TypeCode       = "V",
            Date           = v.StartDate,
            EndDate        = v.EndDate,
            Amount         = -v.TotalAmount,    // negative = lost income
            Description    = v.ChildName != null ? $"VAB · {v.ChildName}" : "VAB",
            MetaLine       = $"{v.TotalDays} dag{(v.TotalDays != 1 ? "ar" : "")} · {v.Rate * 100:N0}% · utebliven inkomst",
            SourceId       = v.Id,
            HasDetail      = false,
            CreatedByEmail = v.UserId
        }).ToList();
    }

    private async Task<List<JournalEntryDto>> FetchReceiptsAsync(JournalQuery q)
    {
        var rq = new ReceiptQuery
        {
            BudgetId        = q.BudgetId,
            Page            = 1,
            PageSize        = int.MaxValue,
            ProjectId       = q.FilterByProject ? q.ProjectId : null,
            Statuses        = q.ReceiptStatuses.Count > 0 ? q.ReceiptStatuses : null,
            CreatedByUserId = q.FilterByCreatedBy ? q.CreatedByUserId : null,
            FromDate        = q.FilterByStartDate ? q.StartDate : null,
            ToDate          = q.FilterByEndDate   ? q.EndDate   : null
        };
        var (batches, _) = await _receiptRepo.GetPagedAsync(rq);
        return batches.Select(b => new JournalEntryDto
        {
            EntryType        = JournalEntryType.ReceiptBatch,
            TypeLabel        = "Kvitto",
            TypeCode         = "K",
            Date             = b.CreatedAt,
            Amount           = b.Lines.Sum(l => l.Amount),
            Description      = b.Label,
            CategoryName     = b.Category?.Name,
            ProjectName      = b.Project?.Name,
            Status           = SwedishReceiptStatus(b.Status),
            ReferenceCode    = $"{DateTime.UtcNow.Year}-{b.Id:D3}-*",
            SourceId         = b.Id,
            HasDetail        = b.Lines.Count > 0,
            ReceiptLineCount = b.Lines.Count,
            MetaLine         = $"{b.Lines.Count} kvitton",
            CreatedByEmail   = b.CreatedByEmail
        }).ToList();
    }

    private async Task<List<JournalEntryDto>> FetchListsAsync(JournalQuery q)
    {
        var lists = await _listRepo.GetAllAsync(q.BudgetId, includeArchived: false);
        return lists.Select(l => new JournalEntryDto
        {
            EntryType      = JournalEntryType.List,
            TypeLabel      = l.ListType == Domain.Enums.ListType.Note ? "Anteckning" : "Lista",
            TypeCode       = l.ListType == Domain.Enums.ListType.Note ? "N" : "L",
            Date           = l.UpdatedAt,
            Amount         = 0,  // lists have zero financial impact
            Description    = l.Name,
            CategoryName   = l.Tags,
            SourceId       = l.Id,
            HasDetail      = l.Items.Count > 0 || l.Content != null,
            MetaLine       = l.ListType == Domain.Enums.ListType.Note
                               ? (l.Content != null ? l.Content[..Math.Min(80, l.Content.Length)] + "…" : null)
                               : $"{l.Items.Count(i => i.IsChecked)}/{l.Items.Count} klara",
            CreatedByEmail = l.CreatedByUserId
        }).ToList();
    }

    private static List<JournalEntryDto> ApplySharedFilters(List<JournalEntryDto> all, JournalQuery q)
    {
        if (q.FilterByDescription && !string.IsNullOrWhiteSpace(q.Description))
            all = all.Where(e =>
                e.Description?.Contains(q.Description, StringComparison.OrdinalIgnoreCase) == true ||
                e.MetaLine?.Contains(q.Description, StringComparison.OrdinalIgnoreCase) == true).ToList();

        if (q.FilterByAmount)
        {
            if (q.AmountMin.HasValue) all = all.Where(e => e.Amount >= q.AmountMin.Value).ToList();
            if (q.AmountMax.HasValue) all = all.Where(e => e.Amount <= q.AmountMax.Value).ToList();
        }

        return all;
    }

    private static string SwedishMilStatus(Domain.Enums.MilersattningStatus s) => s switch
    {
        Domain.Enums.MilersattningStatus.Draft     => "Utkast",
        Domain.Enums.MilersattningStatus.Submitted => "Inskickad",
        Domain.Enums.MilersattningStatus.Approved  => "Godkänd",
        Domain.Enums.MilersattningStatus.Paid      => "Utbetald",
        _ => s.ToString()
    };

    private static string SwedishReceiptStatus(Domain.Enums.ReceiptBatchStatus s) => s switch
    {
        Domain.Enums.ReceiptBatchStatus.Draft      => "Utkast",
        Domain.Enums.ReceiptBatchStatus.Submitted  => "Inskickad",
        Domain.Enums.ReceiptBatchStatus.Approved   => "Godkänd",
        Domain.Enums.ReceiptBatchStatus.Rejected   => "Avslagen",
        Domain.Enums.ReceiptBatchStatus.Reimbursed => "Utbetald",
        _ => s.ToString()
    };
}
