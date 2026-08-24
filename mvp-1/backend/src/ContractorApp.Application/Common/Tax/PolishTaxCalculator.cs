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
    public const decimal TaxReducingAmount = 3_600m;    // kwota zmniejszająca podatek = 12% × kwota wolna
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
    /// Roczny PIT wg skali podatkowej od podstawy (dochód − ZUS społeczny), w pełnych złotych.
    /// Formuła obowiązująca od 2022 r.: 12% × podstawa − kwota zmniejszająca (3600 zł);
    /// powyżej progu: 10 800 + 32% × nadwyżki; nie mniej niż 0. Kwoty wolnej NIE odejmuje się
    /// od podstawy — robi to kwota zmniejszająca. Ta sama formuła co
    /// <see cref="AnnualSettlementCalculator.ScaleTax"/> (tu ze stałymi roku bieżącego).
    /// Używane kumulacyjnie do wyliczenia miesięcznej zaliczki.
    /// </summary>
    public static decimal ScaleAnnualPit(decimal annualTaxableBase)
    {
        if (annualTaxableBase <= 0m) return 0m;
        var tax = annualTaxableBase <= ProgressiveThreshold
            ? annualTaxableBase * ScaleLowerRate - TaxReducingAmount
            : ProgressiveThreshold * ScaleLowerRate - TaxReducingAmount
              + (annualTaxableBase - ProgressiveThreshold) * ScaleUpperRate;
        return Math.Round(Math.Max(0m, tax), 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>VAT naliczony zawarty w kwocie brutto (np. z kosztów z prawem do odliczenia).</summary>
    public static decimal InputVatFromGross(decimal grossAmount, decimal vatRate) =>
        vatRate <= 0m ? 0m : grossAmount * vatRate / (1m + vatRate);

    /// <summary>
    /// Roczny PIT formy bazowej (liniowy/skala) od dochodu pomniejszonego o ZUS społeczny.
    /// Dla skali stosuje kwotę zmniejszającą i próg 32%.
    /// </summary>
    public static decimal BaseAnnualPit(TaxForm form, decimal annualIncome, decimal annualZusSocial)
    {
        var baseAfterZus = Math.Max(0m, annualIncome - annualZusSocial);
        return form == TaxForm.Skala
            ? ScaleAnnualPit(baseAfterZus)
            : baseAfterZus * LinearPitRate; // liniowy 19%
    }

    /// <summary>
    /// Roczny PIT z ulgą IP Box: 5% na dochód kwalifikowany (udział = współczynnik Nexus),
    /// forma bazowa (liniowy 19% lub skala) na pozostałą część dochodu.
    /// </summary>
    public static decimal IpBoxAnnualPit(
        TaxForm form, decimal annualIncome, decimal annualZusSocial, decimal qualifyingShare)
    {
        var q = Math.Clamp(qualifyingShare, 0m, 1m);
        var baseAfterZus = Math.Max(0m, annualIncome - annualZusSocial);
        var ipBase = baseAfterZus * q;
        var nonIpBase = baseAfterZus * (1m - q);
        var nonIpPit = form == TaxForm.Skala
            ? ScaleAnnualPit(nonIpBase)
            : nonIpBase * LinearPitRate;
        return ipBase * IpBoxRate + nonIpPit;
    }
}
