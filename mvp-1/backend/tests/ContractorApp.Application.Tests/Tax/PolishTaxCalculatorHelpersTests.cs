using ContractorApp.Application.Common.Tax;
using ContractorApp.Domain.Enums;
using Xunit;

namespace ContractorApp.Application.Tests.Tax;

/// <summary>
/// Testy pomocniczych metod PolishTaxCalculator: ZUS społeczny, składka zdrowotna,
/// VAT naliczony, miesięczne zaliczki PIT — uzupełnienie testów ScaleAnnualPit
/// i BaseAnnualPit z PolishTaxCalculatorTests.
/// </summary>
public class PolishTaxCalculatorHelpersTests
{
    // ── MonthlyZusSocial ─────────────────────────────────────────────────────

    [Fact]
    public void MonthlyZusSocial_ulga_na_start_zwraca_zero()
        => Assert.Equal(0m, PolishTaxCalculator.MonthlyZusSocial(ZusStage.UlgaNaStart));

    [Fact]
    public void MonthlyZusSocial_preferencyjny_zwraca_478()
        => Assert.Equal(478.00m, PolishTaxCalculator.MonthlyZusSocial(ZusStage.Preferencyjny));

    [Fact]
    public void MonthlyZusSocial_pelny_zwraca_1803_33()
        => Assert.Equal(1_803.33m, PolishTaxCalculator.MonthlyZusSocial(ZusStage.Pelny));

    // ── RyczaltHealthMonth ───────────────────────────────────────────────────

    [Theory]
    [InlineData(0,       419)]   // brak przychodu → niski próg
    [InlineData(60_000,  419)]   // dokładnie na granicy niskiego progu (≤ 60 000)
    [InlineData(60_001,  699)]   // przekroczenie → środkowy próg
    [InlineData(300_000, 699)]   // dokładnie na granicy środkowego progu (≤ 300 000)
    [InlineData(300_001, 1_257)] // przekroczenie → wysoki próg
    [InlineData(500_000, 1_257)] // powyżej 300 000
    public void RyczaltHealthMonth_prog_przychodowy(decimal annualRevenue, decimal expected)
        => Assert.Equal(expected, PolishTaxCalculator.RyczaltHealthMonth(annualRevenue));

    // ── MonthlyHealth – ryczałt ──────────────────────────────────────────────

    [Fact]
    public void MonthlyHealth_ryczalt_niski_przychod_zwraca_stala_kwote_niskiego_progu()
        => Assert.Equal(PolishTaxCalculator.HealthRyczaltLow,
            PolishTaxCalculator.MonthlyHealth(TaxForm.Ryczalt, monthlyIncome: 0m,
                projectedAnnualRevenue: 50_000m));

    [Fact]
    public void MonthlyHealth_ryczalt_sredni_przychod_zwraca_stala_kwote_sredniego_progu()
        => Assert.Equal(PolishTaxCalculator.HealthRyczaltMid,
            PolishTaxCalculator.MonthlyHealth(TaxForm.Ryczalt, monthlyIncome: 0m,
                projectedAnnualRevenue: 150_000m));

    [Fact]
    public void MonthlyHealth_ryczalt_wysoki_przychod_zwraca_stala_kwote_wysokiego_progu()
        => Assert.Equal(PolishTaxCalculator.HealthRyczaltHigh,
            PolishTaxCalculator.MonthlyHealth(TaxForm.Ryczalt, monthlyIncome: 0m,
                projectedAnnualRevenue: 400_000m));

    // ── MonthlyHealth – skala ────────────────────────────────────────────────

    [Fact]
    public void MonthlyHealth_skala_9_procent_dochodu_powyzej_minimum()
    {
        // 9% × 5 000 = 450 PLN > minimum 314 PLN
        Assert.Equal(450m,
            PolishTaxCalculator.MonthlyHealth(TaxForm.Skala, monthlyIncome: 5_000m,
                projectedAnnualRevenue: 0m));
    }

    [Fact]
    public void MonthlyHealth_skala_niski_dochod_zwraca_minimum()
    {
        // 9% × 3 000 = 270 PLN < minimum 314 PLN → minimum
        Assert.Equal(PolishTaxCalculator.HealthMinimum,
            PolishTaxCalculator.MonthlyHealth(TaxForm.Skala, monthlyIncome: 3_000m,
                projectedAnnualRevenue: 0m));
    }

    [Fact]
    public void MonthlyHealth_skala_ujemny_dochod_zwraca_minimum()
        => Assert.Equal(PolishTaxCalculator.HealthMinimum,
            PolishTaxCalculator.MonthlyHealth(TaxForm.Skala, monthlyIncome: -5_000m,
                projectedAnnualRevenue: 0m));

    // ── MonthlyHealth – liniowy ──────────────────────────────────────────────

    [Fact]
    public void MonthlyHealth_liniowy_4_9_procent_dochodu_powyzej_minimum()
    {
        // 4,9% × 10 000 = 490 PLN > minimum 314 PLN
        Assert.Equal(490m,
            PolishTaxCalculator.MonthlyHealth(TaxForm.Liniowy, monthlyIncome: 10_000m,
                projectedAnnualRevenue: 0m));
    }

    [Fact]
    public void MonthlyHealth_liniowy_niski_dochod_zwraca_minimum()
    {
        // 4,9% × 3 000 = 147 PLN < minimum 314 PLN → minimum
        Assert.Equal(PolishTaxCalculator.HealthMinimum,
            PolishTaxCalculator.MonthlyHealth(TaxForm.Liniowy, monthlyIncome: 3_000m,
                projectedAnnualRevenue: 0m));
    }

    [Fact]
    public void MonthlyHealth_liniowy_ujemny_dochod_zwraca_minimum()
        => Assert.Equal(PolishTaxCalculator.HealthMinimum,
            PolishTaxCalculator.MonthlyHealth(TaxForm.Liniowy, monthlyIncome: -2_000m,
                projectedAnnualRevenue: 0m));

    // ── InputVatFromGross ────────────────────────────────────────────────────

    [Fact]
    public void InputVatFromGross_stawka_23_procent_kwota_brutto_123()
    {
        // VAT naliczony: 123 × 0,23 / 1,23 = 23,00 PLN
        Assert.Equal(23m, PolishTaxCalculator.InputVatFromGross(123m, 0.23m));
    }

    [Fact]
    public void InputVatFromGross_zerowa_stawka_zwraca_zero()
        => Assert.Equal(0m, PolishTaxCalculator.InputVatFromGross(1_000m, 0m));

    [Fact]
    public void InputVatFromGross_ujemna_stawka_zwraca_zero()
        => Assert.Equal(0m, PolishTaxCalculator.InputVatFromGross(1_000m, -0.05m));

    [Fact]
    public void InputVatFromGross_zerowa_kwota_zwraca_zero()
        => Assert.Equal(0m, PolishTaxCalculator.InputVatFromGross(0m, 0.23m));

    // ── RyczaltMonthlyPit ────────────────────────────────────────────────────

    [Fact]
    public void RyczaltMonthlyPit_12_procent_przychodu()
        => Assert.Equal(1_200m, PolishTaxCalculator.RyczaltMonthlyPit(10_000m));

    [Fact]
    public void RyczaltMonthlyPit_zerowy_przychod_daje_zero()
        => Assert.Equal(0m, PolishTaxCalculator.RyczaltMonthlyPit(0m));

    [Fact]
    public void RyczaltMonthlyPit_ujemny_przychod_daje_zero()
        => Assert.Equal(0m, PolishTaxCalculator.RyczaltMonthlyPit(-5_000m));

    // ── LinearMonthlyPit ─────────────────────────────────────────────────────

    [Fact]
    public void LinearMonthlyPit_19_procent_od_dochodu_po_zus()
    {
        // (20 000 − 1 000) × 19% = 19 000 × 0,19 = 3 610 PLN
        Assert.Equal(3_610m, PolishTaxCalculator.LinearMonthlyPit(20_000m, 1_000m));
    }

    [Fact]
    public void LinearMonthlyPit_zus_przewyzsza_dochod_daje_zero()
    {
        // dochód 1 000 < ZUS 1 803,33 → max(0, ...) = 0
        Assert.Equal(0m, PolishTaxCalculator.LinearMonthlyPit(1_000m, 1_803.33m));
    }

    [Fact]
    public void LinearMonthlyPit_ujemny_dochod_daje_zero()
        => Assert.Equal(0m, PolishTaxCalculator.LinearMonthlyPit(-5_000m, 0m));

    [Fact]
    public void LinearMonthlyPit_zero_zus_to_pelne_19_procent_dochodu()
    {
        // Ulga na start → ZUS społeczny = 0; 10 000 × 19% = 1 900
        Assert.Equal(1_900m, PolishTaxCalculator.LinearMonthlyPit(10_000m, 0m));
    }
}
