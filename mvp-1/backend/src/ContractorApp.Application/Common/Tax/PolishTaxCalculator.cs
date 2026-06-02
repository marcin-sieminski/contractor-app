using ContractorApp.Domain.Enums;

namespace ContractorApp.Application.Common.Tax;

/// <summary>
/// Wspólny kalkulator polskich obciążeń dla JDG B2B (stawki 2026).
/// Jedno źródło prawdy dla PIT, ZUS, składki zdrowotnej i VAT — używane przez
/// prognozę finansową, podsumowanie finansowe oraz narzędzia podatkowe MCP.
/// Aktualizuj stałe co rok.
/// </summary>
public static class PolishTaxCalculator
{
    // ── ZUS społeczne / miesiąc (2026) ───────────────────────────────────────
    public const decimal ZusSocialFull = 1_803.33m;     // duży ZUS
    public const decimal ZusSocialPreferential = 478.00m; // preferencyjny
    public const decimal ZusSocialReliefStart = 0m;       // ulga na start

    // ── Składka zdrowotna ────────────────────────────────────────────────────
    public const decimal HealthMinimum = 314.00m;
    public const decimal HealthRyczaltLow = 419.00m;    // przychód roczny ≤ 60 000 PLN
    public const decimal HealthRyczaltMid = 699.00m;    // 60 001 – 300 000 PLN
    public const decimal HealthRyczaltHigh = 1_257.00m; // > 300 000 PLN
    public const decimal HealthLinearRate = 0.049m;     // 4,9% dochodu (liniowy)
    public const decimal HealthScaleRate = 0.09m;       // 9,0% dochodu (skala)

    // ── PIT ──────────────────────────────────────────────────────────────────
    public const decimal RyczaltItRate = 0.12m;   // 12% ryczałt usługi IT
    public const decimal LinearPitRate = 0.19m;   // 19% liniowy
    public const decimal IpBoxRate = 0.05m;       // 5% IP Box
    public const decimal TaxFreeAmount = 30_000m;       // kwota wolna (skala)
    public const decimal ProgressiveThreshold = 120_000m; // próg 32% (skala)
    public const decimal ScaleLowerRate = 0.12m;
    public const decimal ScaleUpperRate = 0.32m;

    // ── VAT ────────────────────────────────────────────────────────────────────
    public const decimal VatStandardRate = 0.23m;

    /// <summary>Miesięczna składka społeczna ZUS dla danego etapu.</summary>
    public static decimal MonthlyZusSocial(ZusStage stage) => stage switch
    {
        ZusStage.UlgaNaStart => ZusSocialReliefStart,
        ZusStage.Preferencyjny => ZusSocialPreferential,
        _ => ZusSocialFull
    };

    /// <summary>
    /// Miesięczna składka zdrowotna zależna od formy opodatkowania.
    /// Ryczałt — próg wg prognozowanego przychodu rocznego (kwota stała).
    /// Liniowy/skala — procent dochodu miesięcznego, nie mniej niż minimum.
    /// </summary>
    public static decimal MonthlyHealth(TaxForm form, decimal monthlyIncome, decimal projectedAnnualRevenue) => form switch
    {
        TaxForm.Ryczalt => RyczaltHealthMonth(projectedAnnualRevenue),
        TaxForm.Skala => Math.Max(HealthMinimum, Math.Max(0m, monthlyIncome) * HealthScaleRate),
        _ => Math.Max(HealthMinimum, Math.Max(0m, monthlyIncome) * HealthLinearRate)
    };

    /// <summary>Stała miesięczna składka zdrowotna na ryczałcie wg progu przychodu rocznego.</summary>
    public static decimal RyczaltHealthMonth(decimal annualRevenue) => annualRevenue switch
    {
        <= 60_000m => HealthRyczaltLow,
        <= 300_000m => HealthRyczaltMid,
        _ => HealthRyczaltHigh
    };

    /// <summary>Miesięczny ryczałt (12% przychodu netto).</summary>
    public static decimal RyczaltMonthlyPit(decimal monthlyRevenue) =>
        Math.Max(0m, monthlyRevenue) * RyczaltItRate;

    /// <summary>
    /// Miesięczna zaliczka PIT liniowego: 19% od dochodu pomniejszonego o ZUS społeczny.
    /// </summary>
    public static decimal LinearMonthlyPit(decimal monthlyIncome, decimal monthlyZusSocial) =>
        Math.Max(0m, monthlyIncome - monthlyZusSocial) * LinearPitRate;

    /// <summary>
    /// Roczny PIT wg skali podatkowej od podstawy (dochód − ZUS − kwota wolna).
    /// Używane kumulacyjnie do wyliczenia miesięcznej zaliczki.
    /// </summary>
    public static decimal ScaleAnnualPit(decimal annualTaxableBase)
    {
        if (annualTaxableBase <= 0m) return 0m;
        return annualTaxableBase <= ProgressiveThreshold
            ? annualTaxableBase * ScaleLowerRate
            : ProgressiveThreshold * ScaleLowerRate + (annualTaxableBase - ProgressiveThreshold) * ScaleUpperRate;
    }

    /// <summary>VAT naliczony zawarty w kwocie brutto (np. z kosztów z prawem do odliczenia).</summary>
    public static decimal InputVatFromGross(decimal grossAmount, decimal vatRate) =>
        vatRate <= 0m ? 0m : grossAmount * vatRate / (1m + vatRate);
}
