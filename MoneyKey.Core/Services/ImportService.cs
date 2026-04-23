using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Caching.Memory;
using MoneyKey.Core.DTOs.Import;
using MoneyKey.DAL.Queries;
using MoneyKey.DAL.Repositories.Interfaces;
using MoneyKey.Domain.Enums;
using MoneyKey.Domain.Models;

namespace MoneyKey.Core.Services;

/// <summary>
/// CSV import service without hard-coded bank profiles.
/// Column detection is automatic based on common Swedish/English header names.
/// Users can save their own import templates (Phase 4).
/// </summary>
public class ImportService
{
    private readonly ITransactionRepository _txRepo;
    private readonly IMemoryCache _cache;

    public ImportService(ITransactionRepository txRepo, IMemoryCache cache)
    {
        _txRepo = txRepo;
        _cache = cache;
    }

    // ── Known column name patterns ────────────────────────────────────────────

    private static readonly HashSet<string> KnownDateColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "datum", "date", "bokföringsdag", "bokfoeringsdag", "transaktionsdatum",
        "valuteringsdatum", "handelsdatum", "konteringsdatum", "transaction date",
        "posting date", "value date"
    };

    private static readonly HashSet<string> KnownAmountColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "belopp", "amount", "debet/kredit", "in/ut", "saldoändring", "saldo ändring",
        "in", "ut", "kredit", "debet", "summa", "transaction amount", "debit/credit"
    };

    private static readonly HashSet<string> KnownDescriptionColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "beskrivning", "text", "transaktionstext", "meddelande", "rubrik",
        "transaktion", "mottagare/avsändare", "avsändare", "mottagare",
        "description", "details", "merchant", "payee", "memo", "note", "narration"
    };

    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd", "dd/MM/yyyy", "dd-MM-yyyy", "MM/dd/yyyy",
        "d/M/yyyy", "yyyyMMdd", "yyyy/MM/dd", "dd.MM.yyyy"
    };

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<ImportSessionDto> PreviewAsync(
        Stream fileStream, int budgetId, string userId)
    {
        var rows = ParseCsvAutoDetect(fileStream);

        // Mark probable duplicates
        var existing = await _txRepo.GetForExportAsync(new TransactionQuery
        {
            BudgetId = budgetId,
            FilterByStartDate = true,
            StartDate = rows.Any() ? rows.Min(r => r.Date).AddDays(-1) : DateTime.Today.AddMonths(-6)
        });

        foreach (var row in rows)
        {
            row.IsDuplicate = existing.Any(t =>
                t.StartDate.Date == row.Date.Date &&
                Math.Abs(t.NetAmount - row.Amount) < 0.01m &&
                string.Equals(t.Description?.Trim(), row.Description?.Trim(),
                    StringComparison.OrdinalIgnoreCase));
            row.Selected = !row.IsDuplicate;
        }

        var sessionId = Guid.NewGuid().ToString("N");
        _cache.Set(
            CacheKey(userId, sessionId),
            rows,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) });

        return new ImportSessionDto(sessionId, new ImportPreviewDto
        {
            Rows = rows,
            TotalRows = rows.Count,
            DuplicateCount = rows.Count(r => r.IsDuplicate)
        });
    }

    public async Task<int> ConfirmAsync(ConfirmImportDto dto, int budgetId, string userId)
    {
        if (string.IsNullOrWhiteSpace(dto.SessionId))
            throw new ArgumentException("SessionId saknas.", nameof(dto));

        if (dto.SelectedRowIndices == null || dto.SelectedRowIndices.Count == 0)
            throw new ArgumentException("Inga rader valda för import.", nameof(dto));

        if (!_cache.TryGetValue(CacheKey(userId, dto.SessionId), out List<ImportRowDto>? allRows) || allRows == null)
            throw new InvalidOperationException(
                "Import-sessionen har gått ut eller är ogiltig. Ladda upp filen igen.");

        var selected = allRows.Where(r => dto.SelectedRowIndices.Contains(r.RowIndex)).ToList();

        foreach (var r in selected)
        {
            await _txRepo.CreateAsync(new Transaction
            {
                BudgetId = budgetId,
                StartDate = r.Date,
                NetAmount = r.Amount,
                Description = r.Description,
                CategoryId = dto.DefaultCategoryId,
                Type = r.Amount >= 0 ? TransactionType.Income : TransactionType.Expense,
                Recurrence = Recurrence.OneTime,
                IsActive = true,
                CreatedByUserId = userId
            });
        }

        _cache.Remove(CacheKey(userId, dto.SessionId));
        return selected.Count;
    }

    // ── CSV parsing ───────────────────────────────────────────────────────────

    private static List<ImportRowDto> ParseCsvAutoDetect(Stream stream)
    {
        // Read entire content so we can re-parse after delimiter detection
        string content;
        using (var sr = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
            content = sr.ReadToEnd();

        var delimiter = DetectDelimiter(content);

        using var reader = new StringReader(content);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
            HasHeaderRecord = true,
            BadDataFound = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, config);

        if (!csv.Read() || !csv.ReadHeader()) return new List<ImportRowDto>();

        var headers = csv.HeaderRecord ?? Array.Empty<string>();

        // Locate columns by name
        var dateIdx = FindColumnIndex(headers, KnownDateColumns);
        var amountIdx = FindColumnIndex(headers, KnownAmountColumns);
        var descIdx = FindColumnIndex(headers, KnownDescriptionColumns);

        // Fallback: positional guesses when no named matches found
        if (dateIdx < 0) dateIdx = 0;
        if (amountIdx < 0) amountIdx = headers.Length > 3 ? 3 : headers.Length - 1;
        if (descIdx < 0) descIdx = headers.Length > 1 ? 1 : 0;

        var rows = new List<ImportRowDto>();
        var idx = 0;

        while (csv.Read())
        {
            try
            {
                var dateStr = SafeGet(csv, dateIdx);
                var amountStr = SafeGet(csv, amountIdx);
                var desc = SafeGet(csv, descIdx)?.Trim();

                if (string.IsNullOrWhiteSpace(dateStr) || string.IsNullOrWhiteSpace(amountStr))
                    continue;

                if (!TryParseDate(dateStr, out var date)) continue;
                if (!TryParseAmount(amountStr, out var amount)) continue;

                rows.Add(new ImportRowDto
                {
                    RowIndex = idx++,
                    Date = date,
                    Amount = amount,
                    Description = string.IsNullOrWhiteSpace(desc) ? null : desc,
                    Selected = true
                });
            }
            catch { /* skip malformed rows */ }
        }

        return rows;
    }

    private static char DetectDelimiter(string content)
    {
        // Examine first two lines to count delimiter candidates
        var sample = content.Length > 2000 ? content[..2000] : content;
        var line = sample.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? sample;

        var semicolons = line.Count(c => c == ';');
        var commas = line.Count(c => c == ',');
        var tabs = line.Count(c => c == '\t');

        if (tabs > semicolons && tabs > commas) return '\t';
        if (semicolons >= commas) return ';';
        return ',';
    }

    private static int FindColumnIndex(string[] headers, HashSet<string> knownNames)
    {
        for (var i = 0; i < headers.Length; i++)
            if (knownNames.Contains(headers[i].Trim()))
                return i;
        // Partial match fallback
        for (var i = 0; i < headers.Length; i++)
            foreach (var known in knownNames)
                if (headers[i].Trim().Contains(known, StringComparison.OrdinalIgnoreCase))
                    return i;
        return -1;
    }

    private static string? SafeGet(CsvReader csv, int index)
    {
        try { return index >= 0 ? csv.GetField(index) : null; }
        catch { return null; }
    }

    private static bool TryParseDate(string s, out DateTime date)
    {
        s = s.Trim();
        return DateTime.TryParseExact(s, DateFormats, CultureInfo.InvariantCulture,
                   DateTimeStyles.None, out date)
            || DateTime.TryParse(s, CultureInfo.InvariantCulture,
                   DateTimeStyles.None, out date);
    }

    private static bool TryParseAmount(string s, out decimal amount)
    {
        // Normalise Swedish number format: "1 234,56" or "1.234,56" → "1234.56"
        s = s.Trim()
             .Replace("\u00a0", "")  // non-breaking space
             .Replace(" ", "");

        // If both . and , present, figure out which is decimal separator
        var dotIdx = s.LastIndexOf('.');
        var commaIdx = s.LastIndexOf(',');

        if (commaIdx > dotIdx)
        {
            // Comma is the decimal separator: remove dots (thousands), replace comma with dot
            s = s.Replace(".", "").Replace(",", ".");
        }
        else if (dotIdx > commaIdx && commaIdx >= 0)
        {
            // Dot is the decimal separator: remove commas (thousands)
            s = s.Replace(",", "");
        }
        // else: only dots or only commas or neither — treat as-is

        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
    }

    private static string CacheKey(string userId, string sessionId) =>
        $"import_session:{userId}:{sessionId}";
}