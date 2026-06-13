namespace ContractorApp.Application.Common.Tax;

/// <summary>
/// Komplet parametrów polskiego systemu podatkowo-składkowego dla jednego roku podatkowego.
/// Używany przez kalkulator rozliczenia rocznego; wartości per rok dostarcza
/// <see cref="TaxYearParametersProvider"/>. Stałe w <see cref="PolishTaxCalculator"/>
/// pozostają źródłem dla prognozy bieżącej (stawki 2026).
/// </summary>
public sealed record TaxYearParameters(
    int Year,

    // ZUS społeczne (miesięcznie, z dobrowolnym chorobowym)
    decimal ZusSocialFullMonthly,
    decimal ZusSocialPreferentialMonthly,

    // Składka zdrowotna
    decimal HealthMinimumMonthly,
    decimal HealthRyczaltLowMonthly,    // przychód roczny ≤ 60 000
    decimal HealthRyczaltMidMonthly,    // 60 000 – 300 000
    decimal HealthRyczaltHighMonthly,   // > 300 000
    decimal HealthLinearRate,           // 4,9% dochodu (liniowy)
    decimal HealthScaleRate,            // 9,0% dochodu (skala)

    // PIT
    decimal RyczaltItRate,              // 12% usługi IT (PKWiU 62)
    decimal LinearPitRate,              // 19%
    decimal IpBoxRate,                  // 5%
    decimal TaxFreeAmount,              // kwota wolna (skala)
    decimal TaxReducingAmount,          // kwota zmniejszająca podatek (skala) = 12% × kwota wolna
    decimal ProgressiveThreshold,       // próg 32%
    decimal ScaleLowerRate,
    decimal ScaleUpperRate,

    // Odliczenia składki zdrowotnej w zeznaniu rocznym
    decimal LinearHealthDeductionCap,      // roczny limit odliczenia od dochodu (liniowy), obwieszczenie MF
    decimal RyczaltHealthDeductibleShare,  // udział zapłaconej zdrowotnej odliczany od przychodu (ryczałt)

    decimal VatStandardRate);
