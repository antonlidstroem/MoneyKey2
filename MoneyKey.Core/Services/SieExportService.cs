using System.Text;
using MoneyKey.Core.DTOs.Sie;
using MoneyKey.Core.DTOs.Transaction;
using MoneyKey.Domain.Models;

namespace MoneyKey.Core.Services;

/// <summary>
/// Generates a SIE 4-format file for Swedish bookkeeping systems.
/// SIE4 is accepted by Fortnox, Visma, Bokio, Björn Lundén etc.
/// </summary>
public static class SieExportService
{
    // Default BAS account mappings when no custom mappings exist
    private static readonly Dictionary<string, string> DefaultBasAccounts = new()
    {
        ["Mat"]                = "4010",
        ["Transport"]          = "5800",
        ["HusDrift"]           = "5060",
        ["Fritid"]             = "6992",
        ["Barn"]               = "7699",
        ["StreamingTjänster"]  = "6540",
        ["SaaS-produkter"]     = "6540",
        ["Bidrag"]             = "8314",
        ["Hobbyverksamhet"]    = "3020",
        ["Lön"]                = "7010",
        ["Milersättning"]      = "5800",
        ["VAB/Sjukfrånvaro"]   = "7699",
    };

    public static byte[] Generate(
        SieExportRequestDto req,
        List<TransactionDto> transactions,
        List<CategoryAccountMapping> mappings)
    {
        var sb  = new StringBuilder();
        var now = DateTime.Now;

        // ── Header ─────────────────────────────────────────────────────────────
        sb.AppendLine("#FLAGGA 0");
        sb.AppendLine($"#PROGRAM \"MoneyKey\" \"1.0\"");
        sb.AppendLine($"#FORMAT PC8");
        sb.AppendLine($"#GEN {now:yyyyMMdd}");
        sb.AppendLine($"#SIETYP 4");
        sb.AppendLine($"#FNAMN \"{Escape(req.CompanyName)}\"");
        if (!string.IsNullOrWhiteSpace(req.OrgNumber))
            sb.AppendLine($"#ORGNR {req.OrgNumber}");
        sb.AppendLine($"#RAR 0 {req.Year}0101 {req.Year}1231");
        sb.AppendLine($"#KPTYP BAS2020");
        sb.AppendLine();

        // ── Account plan (from mappings) ───────────────────────────────────────
        var usedAccounts = new HashSet<string>();
        var accountMap   = BuildAccountMap(mappings);

        // ── Vouchers ───────────────────────────────────────────────────────────
        var filtered = transactions
            .Where(t => t.StartDate.Year == req.Year && t.NetAmount != 0)
            .OrderBy(t => t.StartDate)
            .ToList();

        int verNr = 1;
        foreach (var tx in filtered)
        {
            var account = accountMap.TryGetValue(tx.CategoryName, out var acc) ? acc
                        : DefaultBasAccounts.TryGetValue(tx.CategoryName, out var def) ? def
                        : tx.Type == MoneyKey.Domain.Enums.TransactionType.Income ? "3000" : "4000";

            var contraAccount = tx.Type == MoneyKey.Domain.Enums.TransactionType.Income ? "1930" : "2440";
            var amount        = tx.NetAmount;
            var date          = tx.StartDate.ToString("yyyyMMdd");
            var desc          = Escape(tx.Description ?? tx.CategoryName ?? "");

            usedAccounts.Add(account);
            usedAccounts.Add(contraAccount);

            sb.AppendLine($"#VER \"A\" {verNr} {date} \"{desc}\"");
            sb.AppendLine("{");
            sb.AppendLine($"   #TRANS {account}    {{}} {F(amount)} {date} \"{desc}\"");
            sb.AppendLine($"   #TRANS {contraAccount} {{}} {F(-amount)} {date} \"{desc}\"");
            sb.AppendLine("}");
            verNr++;
        }

        // ── Account definitions (prepend) ──────────────────────────────────────
        var accountDefs = new StringBuilder();
        foreach (var acc in usedAccounts.OrderBy(x => x))
            accountDefs.AppendLine($"#KONTO {acc} \"{GetAccountName(acc, mappings)}\"");

        var final = accountDefs.ToString() + "\n" + sb.ToString();
        return Encoding.GetEncoding("windows-1252").GetBytes(final);
    }

    private static Dictionary<string, string> BuildAccountMap(List<CategoryAccountMapping> mappings) =>
        mappings.ToDictionary(m => m.Category?.Name ?? "", m => m.BasAccount);

    private static string GetAccountName(string acc, List<CategoryAccountMapping> mappings)
    {
        var m = mappings.FirstOrDefault(x => x.BasAccount == acc);
        if (m != null) return m.AccountName;
        return acc switch
        {
            "1930" => "Företagskonto / checkkonto",
            "2440" => "Leverantörsskulder",
            "3000" => "Försäljning, varor",
            "4000" => "Inköp varor",
            "4010" => "Inköp varor, handel",
            "5060" => "Hyra lokaler",
            "5800" => "Resekostnader",
            "6540" => "IT-tjänster och licensavgifter",
            "6992" => "Övriga externa kostnader",
            "7010" => "Löner tjänstemän",
            "7699" => "Övriga personalkostnader",
            "8314" => "Ränteintäkter",
            _      => "Konto"
        };
    }

    private static string Escape(string s) => s.Replace("\"", "'").Replace("\n", " ");
    private static string F(decimal v) => v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
}
