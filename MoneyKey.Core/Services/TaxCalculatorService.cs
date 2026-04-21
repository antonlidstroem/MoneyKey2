using MoneyKey.Core.DTOs.Tax;

namespace MoneyKey.Core.Services;

/// <summary>
/// Swedish income tax calculator.
/// Rates for 2025: State tax 20% above 598 500 kr/year.
/// Municipal tax default 32% (configurable per user).
/// </summary>
public static class TaxCalculatorService
{
    // 2025 thresholds
    private const decimal StateTaxThreshold2025 = 598_500m;
    private const decimal StateTaxRate           = 0.20m;

    public static TaxCalculationResultDto Calculate(TaxCalculationRequestDto req)
    {
        var gross    = req.GrossIncome;
        var munRate  = req.MunicipalTaxRate / 100m;
        var year     = req.Year;

        // Basic deduction (grundavdrag) — simplified linear approximation 2025
        // Full grundavdrag: 47 100–49 700 for incomes 135 000–490 000
        var grundavdrag = gross switch
        {
            <= 0                => 0m,
            <= 135_000          => gross * 0.20m,
            <= 490_000          => 49_700m,
            <= 750_000          => 49_700m - (gross - 490_000m) * 0.10m,
            _                   => 14_700m
        };
        grundavdrag = Math.Round(Math.Max(grundavdrag, 14_700m), 0);

        // Job tax credit (jobbskatteavdrag) — approximation for employees
        var jsa = req.IsFreelancer ? 0m : CalculateJobTaxCredit(gross, munRate);

        var taxableIncome   = Math.Max(0, gross - grundavdrag);
        var municipalTax    = Math.Round(taxableIncome * munRate, 0);
        var stateTax        = taxableIncome > StateTaxThreshold2025
            ? Math.Round((taxableIncome - StateTaxThreshold2025) * StateTaxRate, 0)
            : 0m;
        var totalTaxBefore  = municipalTax + stateTax;
        var totalTax        = Math.Max(0, totalTaxBefore - jsa);
        var netIncome       = gross - totalTax;
        var effectiveRate   = gross > 0 ? Math.Round(totalTax / gross * 100, 1) : 0m;

        // Freelancer: egenavgifter 28.97% on surplus
        var socialFees = req.IsFreelancer ? Math.Round(gross * 0.2897m, 0) : 0m;

        return new TaxCalculationResultDto
        {
            GrossIncome      = gross,
            BasicDeduction   = grundavdrag,
            JobTaxCredit     = jsa,
            TaxableIncome    = taxableIncome,
            MunicipalTax     = municipalTax,
            StateTax         = stateTax,
            TotalTax         = totalTax,
            NetIncome        = netIncome,
            EffectiveTaxRate = effectiveRate,
            SocialFees       = socialFees,
            TaxYear          = year.ToString()
        };
    }

    private static decimal CalculateJobTaxCredit(decimal gross, decimal munRate)
    {
        // Simplified JSA 2025
        if (gross <= 0) return 0;
        decimal pbl = 57_300m; // prisbasbelopp 2025
        decimal basis = gross > pbl ? pbl + (gross - pbl) * 0.25m : gross;
        return Math.Round(Math.Min(basis * munRate * 0.835m, 50_000m), 0);
    }
}
