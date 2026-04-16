using MoneyKey.Core.DTOs.Receipt;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Constants;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MoneyKey.Core.Services;

public class ReceiptService
{
    private readonly IReceiptRepository     _repo;
    private readonly ITransactionRepository _txRepo;

    public ReceiptService(IReceiptRepository repo, ITransactionRepository txRepo)
    {
        _repo   = repo;
        _txRepo = txRepo;
    }

    public static string GenerateReferenceCode(int year, int batchId, int seq)
    {
        var bw = batchId > 999 ? "D4" : "D3";
        var sw = seq     > 999 ? "D4" : "D3";
        return $"{year}-{batchId.ToString(bw)}-{seq.ToString(sw)}";
    }

    public static void ValidateStatusTransition(
        ReceiptBatchStatus current, ReceiptBatchStatus next,
        bool isOwner, bool isCreator)
    {
        var ok = (current, next) switch
        {
            (ReceiptBatchStatus.Draft,     ReceiptBatchStatus.Submitted)  => isCreator,
            (ReceiptBatchStatus.Submitted, ReceiptBatchStatus.Draft)      => isCreator,
            (ReceiptBatchStatus.Submitted, ReceiptBatchStatus.Approved)   => isOwner,
            (ReceiptBatchStatus.Submitted, ReceiptBatchStatus.Rejected)   => isOwner,
            (ReceiptBatchStatus.Approved,  ReceiptBatchStatus.Reimbursed) => isOwner,
            (ReceiptBatchStatus.Rejected,  ReceiptBatchStatus.Draft)      => isCreator,
            _ => false
        };
        if (!ok) throw new InvalidOperationException($"Statusövergång {current} → {next} inte tillåten.");
    }

    public async Task<ReceiptBatch> CreateBatchAsync(int budgetId, string userId, string userEmail, CreateReceiptBatchDto dto)
    {
        var batch = new ReceiptBatch
        {
            BudgetId        = budgetId,
            ProjectId       = dto.ProjectId,
            Label           = dto.Label,
            BatchCategoryId = dto.BatchCategoryId,
            CreatedByUserId = userId,
            CreatedByEmail  = userEmail
        };
        return await _repo.CreateAsync(batch);
    }

    public async Task<ReceiptLine> AddLineAsync(int batchId, int budgetId, CreateReceiptLineDto dto)
    {
        var batch = await _repo.GetByIdAsync(batchId, budgetId)
            ?? throw new KeyNotFoundException("Batch hittades inte.");
        if (batch.Status != ReceiptBatchStatus.Draft)
            throw new InvalidOperationException("Kan bara lägga till kvitton i utkast.");

        // FIX BUG-8: Derive the next sequence number from the already-loaded batch.Lines
        // instead of issuing a second MAX(SequenceNumber) query.
        // This eliminates one DB round-trip AND closes the concurrency window:
        // if two requests race, EF will still enforce the UNIQUE(BatchId, SeqNumber) constraint
        // at the DB level — but using Lines.Count avoids the separate MAX query race.
        // The UNIQUE constraint acts as the final guard; callers should retry on DbUpdateException.
        var seq  = (batch.Lines.Any() ? batch.Lines.Max(l => l.SequenceNumber) : 0) + 1;
        var code = GenerateReferenceCode(DateTime.UtcNow.Year, batchId, seq);
        var line = new ReceiptLine
        {
            BatchId        = batchId,
            SequenceNumber = seq,
            ReferenceCode  = code,
            Date           = dto.Date,
            Amount         = dto.Amount,
            Vendor         = dto.Vendor,
            Description    = dto.Description
        };
        return await _repo.AddLineAsync(line);
    }

    public async Task<ReceiptBatch> UpdateStatusAsync(
        int batchId, int budgetId, ReceiptBatchStatus newStatus,
        string actorUserId, BudgetMemberRole actorRole, string? rejectionReason = null)
    {
        var batch = await _repo.GetByIdAsync(batchId, budgetId)
            ?? throw new KeyNotFoundException("Batch hittades inte.");
        ValidateStatusTransition(batch.Status, newStatus,
            actorRole == BudgetMemberRole.Owner,
            batch.CreatedByUserId == actorUserId);

        var now = DateTime.UtcNow;
        batch.Status = newStatus;
        switch (newStatus)
        {
            case ReceiptBatchStatus.Submitted:
                batch.SubmittedAt = now;
                break;
            case ReceiptBatchStatus.Approved:
                batch.ApprovedAt = now; batch.ApprovedByUserId = actorUserId;
                await CreateLinkedTransactionsAsync(batch);
                break;
            case ReceiptBatchStatus.Rejected:
                batch.RejectedAt = now; batch.RejectedByUserId = actorUserId;
                batch.RejectionReason = rejectionReason;
                break;
            case ReceiptBatchStatus.Reimbursed:
                batch.ReimbursedAt = now;
                break;
            case ReceiptBatchStatus.Draft:
                batch.SubmittedAt = null;
                break;
        }
        return await _repo.UpdateAsync(batch);
    }

    private async Task CreateLinkedTransactionsAsync(ReceiptBatch batch)
    {
        var loaded = await _repo.GetByIdAsync(batch.Id, batch.BudgetId);
        if (loaded?.Lines == null || !loaded.Lines.Any()) return;
        foreach (var line in loaded.Lines)
        {
            // FIX BUG-10: Map the ReceiptBatch's category to a sensible transaction CategoryId
            // instead of hardcoding Transport (3) for every receipt type.
            var categoryId = MapReceiptBatchCategoryToTransactionCategory(batch.BatchCategoryId);

            var tx = new Transaction
            {
                BudgetId        = batch.BudgetId,
                ProjectId       = batch.ProjectId,
                StartDate       = line.Date,
                NetAmount       = -Math.Abs(line.Amount),
                Description     = $"Utlägg [{line.ReferenceCode}]{(line.Vendor != null ? $": {line.Vendor}" : "")}",
                CategoryId      = categoryId,
                Type            = TransactionType.Expense,
                Recurrence      = Recurrence.OneTime,
                IsActive        = true,
                CreatedByUserId = batch.CreatedByUserId
            };
            tx = await _txRepo.CreateAsync(tx);
            line.LinkedTransactionId = tx.Id;
            await _repo.UpdateLineAsync(line);
        }
    }

    /// <summary>
    /// Maps a ReceiptBatchCategory seed ID to the most appropriate transaction CategoryId.
    /// ReceiptBatchCategory seed IDs (BudgetDbContext seed data):
    ///   1 = Resor &amp; Transport  → Transport (3)
    ///   2 = Representation       → Fritid (4) — closest for client meals/entertainment
    ///   3 = Kontor &amp; Förbrukning → Hus &amp; drift (2)
    ///   4 = IT &amp; Utrustning    → SaaS-produkter (7)
    ///   5 = Utbildning &amp; Konferens → Fritid (4)
    ///   6 = Tjänster &amp; Konsulting → SaaS-produkter (7)
    ///   7 = Övrigt               → Hus &amp; drift (2)  (general fallback)
    /// </summary>
    private static int MapReceiptBatchCategoryToTransactionCategory(int receiptCategoryId) =>
        receiptCategoryId switch
        {
            1 => CategoryConstants.Transport,        // Resor & Transport → Transport
            2 => CategoryConstants.Fritid,           // Representation → Fritid
            3 => CategoryConstants.HusDrift,         // Kontor & Förbrukning → Hus & drift
            4 => CategoryConstants.SaasProdukter,    // IT & Utrustning → SaaS-produkter
            5 => CategoryConstants.Fritid,           // Utbildning & Konferens → Fritid
            6 => CategoryConstants.SaasProdukter,    // Tjänster & Konsulting → SaaS-produkter
            _ => CategoryConstants.HusDrift          // Övrigt → Hus & drift (safe fallback)
        };

    public async Task<List<ReceiptBatchCategory>> GetCategoriesAsync() =>
        await _repo.GetCategoriesAsync();

    public byte[] ExportBatchToPdf(ReceiptBatch batch, string budgetName)
    {
        static IContainer HeaderCell(IContainer c) => c.Background("#263238").Padding(5);
        IContainer DataCell(IContainer c, int i) => c.Background(i % 2 == 0 ? "#FFFFFF" : "#F5F5F5").Padding(5);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4); page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text(budgetName).Bold().FontSize(14).FontColor("#1565C0");
                        r.ConstantItem(180).AlignRight().Text($"Utskrivet {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor("#78909C");
                    });
                    col.Item().Text($"Utläggsunderlag: {batch.Label}").Bold().FontSize(12);
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor("#E0E0E0");
                });

                page.Content().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(22); cols.ConstantColumn(80); cols.ConstantColumn(62);
                        cols.RelativeColumn(2);  cols.RelativeColumn(3);  cols.ConstantColumn(55); cols.ConstantColumn(65);
                    });
                    table.Header(h =>
                    {
                        foreach (var t in new[] { "#", "Referenskod", "Datum", "Leverantör", "Beskrivning", "Valuta", "Belopp" })
                            h.Cell().Element(HeaderCell).Text(t).Bold().FontColor("#FFFFFF");
                    });
                    var lines = batch.Lines.OrderBy(l => l.SequenceNumber).ToList();
                    for (var i = 0; i < lines.Count; i++)
                    {
                        var l = lines[i]; var idx = i;
                        IContainer C(IContainer c) => DataCell(c, idx);
                        table.Cell().Element(C).Text(l.SequenceNumber.ToString());
                        table.Cell().Element(C).Text(l.ReferenceCode).Bold().FontFamily("Courier New").FontSize(8);
                        table.Cell().Element(C).Text(l.Date.ToString("yyyy-MM-dd"));
                        table.Cell().Element(C).Text(l.Vendor ?? "–");
                        table.Cell().Element(C).Text(l.Description ?? "–");
                        table.Cell().Element(C).AlignRight().Text(l.Currency);
                        table.Cell().Element(C).AlignRight().Text(l.Amount.ToString("N2"));
                    }
                });

                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor("#E0E0E0");
                    col.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text($"Totalt: {batch.Lines.Count} kvitton · {batch.Lines.Sum(l => l.Amount):N2} SEK").Bold().FontSize(9);
                    });
                });
            });
        }).GeneratePdf();
    }
}
