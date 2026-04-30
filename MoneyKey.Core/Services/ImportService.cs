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
/// CSV import service.
/// Auto-detect mode: column names are matched against known Swedish/English patterns.
/// Manual mode: caller provides zero-based column indices for date, amount, description.
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
        "posting date", "value date", "transaktionsdag", "bokdatum"
    };

    private static readonly HashSet<string> KnownAmountColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "belopp", "amount", "debet/kredit", "in/ut", "saldoändring", "saldo ändring",
        "in", "ut", "kredit", "debet", "summa", "transaction amount", "debit/credit",
        "belopp (sek)", "amount (sek)", "netto"
    };

    private static readonly HashSet<string> KnownDescriptionColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "beskrivning", "text", "transaktionstext", "meddelande", "rubrik",
        "transaktion", "mottagare/avsändare", "avsändare", "mottagare",
        "description", "details", "merchant", "payee", "memo", "note", "narration",
        "information", "detaljer"
    };

    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd", "dd/MM/yyyy", "dd-MM-yyyy", "MM/dd/yyyy",
        "d/M/yyyy",   "yyyyMMdd",   "yyyy/MM/dd", "dd.MM.yyyy",
        "d.M.yyyy",   "d/M/yy"
    };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Auto-detect columns and return preview.</summary>
    public async Task<ImportSessionDto> PreviewAsync(
        Stream fileStream, int budgetId, string userId)
    {
        var rows = ParseCsvAutoDetect(fileStream);
        return await BuildSessionAsync(rows, budgetId, userId);
    }

    /// <summary>Use caller-provided column indices and return preview.</summary>
    public async Task<ImportSessionDto> PreviewWithMappingAsync(
        Stream fileStream, int budgetId, string userId,
        int dateColIndex, int amountColIndex, int descColIndex)
    {
        var rows = ParseCsvWithMapping(fileStream, dateColIndex, amountColIndex, descColIndex);
        return await BuildSessionAsync(rows, budgetId, userId);
    }

    private async Task<ImportSessionDto> BuildSessionAsync(
        List<ImportRowDto> rows, int budgetId, string userId)
    {
        // Mark probable duplicates
        if (rows.Any())
        {
            var existing = await _txRepo.GetForExportAsync(new TransactionQuery
            {
                BudgetId = budgetId,
                FilterByStartDate = true,
                StartDate = rows.Min(r => r.Date).AddDays(-1)
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
        }

        var sessionId = Guid.NewGuid().ToString("N");
        _cache.Set(
            CacheKey(userId, sessionId),
            rows,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

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

    // ── CSV parsing — auto-detect ─────────────────────────────────────────────

    private static List<ImportRowDto> ParseCsvAutoDetect(Stream stream)
    {
        string content;
        using (var sr = new StreamReader(stream, Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true, leaveOpen: true))
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

        var dateIdx = FindColumnIndex(headers, KnownDateColumns);
        var amountIdx = FindColumnIndex(headers, KnownAmountColumns);
        var descIdx = FindColumnIndex(headers, KnownDescriptionColumns);

        // Positional fallbacks
        if (dateIdx < 0) dateIdx = 0;
        if (amountIdx < 0) amountIdx = headers.Length > 3 ? 3 : headers.Length - 1;
        if (descIdx < 0) descIdx = headers.Length > 1 ? 1 : 0;

        return ReadRows(csv, dateIdx, amountIdx, descIdx);
    }

    // ── CSV parsing — manual column mapping ───────────────────────────────────

    private static List<ImportRowDto> ParseCsvWithMapping(
        Stream stream, int dateColIndex, int amountColIndex, int descColIndex)
    {
        string content;
        using (var sr = new StreamReader(stream, Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true, leaveOpen: true))
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

        return ReadRows(csv, dateColIndex, amountColIndex, descColIndex);
    }

    private static List<ImportRowDto> ReadRows(
        CsvReader csv, int dateIdx, int amountIdx, int descIdx)
    {
        var rows = new List<ImportRowDto>();
        var idx = 0;

        while (csv.Read())
        {
            try
            {
                var dateStr = SafeGet(csv, dateIdx);
                var amountStr = SafeGet(csv, amountIdx);
                var desc = descIdx >= 0 ? SafeGet(csv, descIdx)?.Trim() : null;

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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static char DetectDelimiter(string content)
    {
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
        // Partial-match fallback
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
        s = s.Trim()
             .Replace("\u00a0", "")
             .Replace(" ", "");

        var dotIdx = s.LastIndexOf('.');
        var commaIdx = s.LastIndexOf(',');

        if (commaIdx > dotIdx)
            s = s.Replace(".", "").Replace(",", ".");
        else if (dotIdx > commaIdx && commaIdx >= 0)
            s = s.Replace(",", "");

        return decimal.TryParse(s, NumberStyles.Any,
            CultureInfo.InvariantCulture, out amount);
    }

    private static string CacheKey(string userId, string sessionId) =>
        $"import_session:{userId}:{sessionId}";
}