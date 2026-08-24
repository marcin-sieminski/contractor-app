namespace ContractorApp.Application.Common.Tax;

/// <summary>
/// Tabele parametrów podatkowo-składkowych per rok. Rozliczenie roczne za rok N
/// musi liczyć wg stawek roku N, nie bieżących. Nieznany rok → najbliższa tabela
/// z flagą IsExact=false (UI/PDF pokazują ostrzeżenie).
/// </summary>
public static class TaxYearParametersProvider
{
    private static readonly IReadOnlyDictionary<int, TaxYearParameters> Tables =
        new Dictionary<int, TaxYearParameters>
        {
            [2025] = new(
                Year: 2025,
                // Podstawa 5203,80 zł (60% prognozowanego przeciętnego wynagrodzenia), z chorobowym. VERIFY
                ZusSocialFullMonthly: 1_646.47m,
                // Podstawa 1399,80 zł (30% minimalnego wynagrodzenia 4666 zł), z chorobowym. VERIFY
                ZusSocialPreferentialMonthly: 442.90m,
                // 9% × 75% minimalnego wynagrodzenia (4666 zł). VERIFY
                HealthMinimumMonthly: 314.96m,
                HealthRyczaltLowMonthly: 461.66m,    // VERIFY (obwieszczenia ZUS 2025)
                HealthRyczaltMidMonthly: 769.43m,    // VERIFY
                HealthRyczaltHighMonthly: 1_384.97m, // VERIFY
                HealthLinearRate: 0.049m,
                HealthScaleRate: 0.09m,
                RyczaltItRate: 0.12m,
                LinearPitRate: 0.19m,
                IpBoxRate: 0.05m,
                TaxFreeAmount: 30_000m,
                TaxReducingAmount: 3_600m,
                ProgressiveThreshold: 120_000m,
                ScaleLowerRate: 0.12m,
                ScaleUpperRate: 0.32m,
                // Obwieszczenie MF: limit odliczenia składki zdrowotnej (liniowy) na 2025. VERIFY
                LinearHealthDeductionCap: 12_900m,
                RyczaltHealthDeductibleShare: 0.50m,
                VatStandardRate: 0.23m),

            // 2026 — spójnie ze stałymi PolishTaxCalculator (jedno źródło stawek bieżących).
            [2026] = new(
                Year: 2026,
                ZusSocialFullMonthly: PolishTaxCalculator.ZusSocialFull,
                ZusSocialPreferentialMonthly: PolishTaxCalculator.ZusSocialPreferential,
                HealthMinimumMonthly: PolishTaxCalculator.HealthMinimum,
                HealthRyczaltLowMonthly: PolishTaxCalculator.HealthRyczaltLow,
                HealthRyczaltMidMonthly: PolishTaxCalculator.HealthRyczaltMid,
                HealthRyczaltHighMonthly: PolishTaxCalculator.HealthRyczaltHigh,
                HealthLinearRate: PolishTaxCalculator.HealthLinearRate,
                HealthScaleRate: PolishTaxCalculator.HealthScaleRate,
                RyczaltItRate: PolishTaxCalculator.RyczaltItRate,
                LinearPitRate: PolishTaxCalculator.LinearPitRate,
                IpBoxRate: PolishTaxCalculator.IpBoxRate,
                TaxFreeAmount: PolishTaxCalculator.TaxFreeAmount,
                TaxReducingAmount: PolishTaxCalculator.TaxReducingAmount,
                ProgressiveThreshold: PolishTaxCalculator.ProgressiveThreshold,
                ScaleLowerRate: PolishTaxCalculator.ScaleLowerRate,
                ScaleUpperRate: PolishTaxCalculator.ScaleUpperRate,
                // Limit na 2026 nieogłoszony w momencie implementacji — szacunek. VERIFY przed rozliczeniem 2026.
                LinearHealthDeductionCap: 14_100m,
                RyczaltHealthDeductibleShare: 0.50m,
                VatStandardRate: PolishTaxCalculator.VatStandardRate)
        };

    /// <summary>
    /// Parametry dla roku podatkowego. Brak tabeli dla roku → najbliższa dostępna
    /// (IsExact=false) — wynik traktować jako orientacyjny.
    /// </summary>
    public static (TaxYearParameters Parameters, bool IsExact) ForYear(int year)
    {
        if (Tables.TryGetValue(year, out var exact))
            return (exact, true);

        var nearestYear = Tables.Keys
            .OrderBy(y => Math.Abs(y - year))
            .ThenByDescending(y => y) // przy remisie preferuj nowszą tabelę
            .First();
        return (Tables[nearestYear], false);
    }

    public static IReadOnlyCollection<int> KnownYears => Tables.Keys.OrderBy(y => y).ToArray();
}
