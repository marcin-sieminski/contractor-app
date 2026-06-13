using ContractorApp.Application.Common.Tax;
using ContractorApp.Domain.Enums;
using Xunit;

namespace ContractorApp.Application.Tests.Tax;

/// <summary>
/// Skala podatkowa w PolishTaxCalculator (stawki 2026): formuła z kwotą zmniejszającą
/// 3600 zł, bez odejmowania kwoty wolnej od podstawy — spójna z
/// AnnualSettlementCalculator.ScaleTax. Wartości zweryfikowane z podatki.gov.pl.
/// </summary>
public class PolishTaxCalculatorTests
{
    [Theory]
    [InlineData(100_242, 8_429)]  // 12% × 100 242 − 3600 = 8 429,04 → 8 429
    [InlineData(120_000, 10_800)] // dokładnie na progu: 14 400 − 3600
    [InlineData(200_000, 36_400)] // 10 800 + 32% × 80 000
    [InlineData(30_000, 0)]       // 3600 − 3600 = 0
    [InlineData(20_000, 0)]       // kwota zmniejszająca większa niż 12% podstawy → 0
    [InlineData(0, 0)]
    [InlineData(-5_000, 0)]
    public void ScaleAnnualPit_formula_z_kwota_zmniejszajaca(decimal podstawa, decimal expected)
        => Assert.Equal(expected, PolishTaxCalculator.ScaleAnnualPit(podstawa));

    [Fact]
    public void ScaleAnnualPit_prog_32_procent_nie_jest_przesuniety()
    {
        // Regresja: wcześniejsze odjęcie kwoty wolnej od podstawy przesuwało próg 32%
        // ze 120 000 na 150 000 zł i zaniżało podatek o stałe 6000 zł powyżej progu.
        // podstawa 150 000 → 10 800 + 32% × 30 000 = 20 400 (błędna formuła dałaby 14 400).
        Assert.Equal(20_400m, PolishTaxCalculator.ScaleAnnualPit(150_000m));
    }

    [Fact]
    public void BaseAnnualPit_skala_odejmuje_od_dochodu_tylko_zus()
    {
        // dochód 139 757,64 − ZUS 19 757,64 = podstawa 120 000 → 10 800.
        Assert.Equal(10_800m,
            PolishTaxCalculator.BaseAnnualPit(TaxForm.Skala, 139_757.64m, 19_757.64m));
    }

    [Fact]
    public void BaseAnnualPit_liniowy_19_procent_od_dochodu_po_zus()
    {
        Assert.Equal(19_000m,
            PolishTaxCalculator.BaseAnnualPit(TaxForm.Liniowy, 110_000m, 10_000m));
    }

    [Fact]
    public void IpBoxAnnualPit_zerowy_udzial_ip_rowna_sie_formie_bazowej()
    {
        var baseline = PolishTaxCalculator.BaseAnnualPit(TaxForm.Skala, 250_000m, 21_640m);
        Assert.Equal(baseline,
            PolishTaxCalculator.IpBoxAnnualPit(TaxForm.Skala, 250_000m, 21_640m, 0m));
    }

    [Fact]
    public void IpBoxAnnualPit_pelny_udzial_ip_to_5_procent_podstawy()
    {
        // 100% dochodu kwalifikowanego: 5% × (200 000 − 20 000) = 9 000.
        Assert.Equal(9_000m,
            PolishTaxCalculator.IpBoxAnnualPit(TaxForm.Liniowy, 200_000m, 20_000m, 1m));
    }
}
