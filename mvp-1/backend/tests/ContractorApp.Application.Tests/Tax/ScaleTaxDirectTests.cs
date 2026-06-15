using ContractorApp.Application.Common.Tax;
using Xunit;

namespace ContractorApp.Application.Tests.Tax;

/// <summary>
/// Bezpośrednie testy AnnualSettlementCalculator.ScaleTax z parametrami roku podatkowego.
/// Uzupełniają PolishTaxCalculatorTests (testujące ScaleAnnualPit ze stałymi bieżącego roku)
/// i weryfikują że ScaleTax poprawnie korzysta z TaxYearParameters — kluczowe dla rozliczeń
/// historycznych, gdy stawki różnią się od bieżącego roku.
/// </summary>
public class ScaleTaxDirectTests
{
    private static readonly TaxYearParameters P2025 = TaxYearParametersProvider.ForYear(2025).Parameters;
    private static readonly TaxYearParameters P2026 = TaxYearParametersProvider.ForYear(2026).Parameters;

    // ── Przypadki graniczne ──────────────────────────────────────────────────

    [Fact]
    public void Zerowa_podstawa_daje_zero()
        => Assert.Equal(0m, AnnualSettlementCalculator.ScaleTax(0m, P2025));

    [Fact]
    public void Ujemna_podstawa_daje_zero()
        => Assert.Equal(0m, AnnualSettlementCalculator.ScaleTax(-10_000m, P2025));

    // ── Pierwszy próg (12%) z kwotą zmniejszającą ────────────────────────────

    [Fact]
    public void Podstawa_30000_kwota_zmniejszajaca_niweluje_podatek()
    {
        // 12% × 30 000 − 3 600 = 3 600 − 3 600 = 0
        Assert.Equal(0m, AnnualSettlementCalculator.ScaleTax(30_000m, P2025));
    }

    [Fact]
    public void Podstawa_50000_podatek_w_pierwszym_progu()
    {
        // 12% × 50 000 − 3 600 = 6 000 − 3 600 = 2 400
        Assert.Equal(2_400m, AnnualSettlementCalculator.ScaleTax(50_000m, P2025));
    }

    [Fact]
    public void Podstawa_100000_podatek_w_pierwszym_progu()
    {
        // 12% × 100 000 − 3 600 = 12 000 − 3 600 = 8 400
        Assert.Equal(8_400m, AnnualSettlementCalculator.ScaleTax(100_000m, P2025));
    }

    // ── Próg 120 000 PLN ─────────────────────────────────────────────────────

    [Fact]
    public void Podstawa_dokladnie_na_progu_120000()
    {
        // 12% × 120 000 − 3 600 = 14 400 − 3 600 = 10 800
        Assert.Equal(10_800m, AnnualSettlementCalculator.ScaleTax(120_000m, P2025));
    }

    // ── Drugi próg (32%) ─────────────────────────────────────────────────────

    [Fact]
    public void Podstawa_150000_podatek_powyzej_progu()
    {
        // 10 800 + 32% × (150 000 − 120 000) = 10 800 + 9 600 = 20 400
        Assert.Equal(20_400m, AnnualSettlementCalculator.ScaleTax(150_000m, P2025));
    }

    [Fact]
    public void Podstawa_200000_podatek_powyzej_progu()
    {
        // 10 800 + 32% × (200 000 − 120 000) = 10 800 + 25 600 = 36 400
        Assert.Equal(36_400m, AnnualSettlementCalculator.ScaleTax(200_000m, P2025));
    }

    // ── Spójność 2025 / 2026 ─────────────────────────────────────────────────

    [Theory]
    [InlineData(80_000)]
    [InlineData(120_000)]
    [InlineData(250_000)]
    public void Wynik_dla_2025_i_2026_sa_identyczne_gdy_stawki_takie_same(decimal podstawa)
    {
        // Stawki 2025 i 2026 są tożsame (próg 120 000, stawki 12%/32%, kwota zmniejszająca 3 600 PLN)
        Assert.Equal(
            AnnualSettlementCalculator.ScaleTax(podstawa, P2025),
            AnnualSettlementCalculator.ScaleTax(podstawa, P2026));
    }

    // ── Zaokrąglenie do pełnych złotych ─────────────────────────────────────

    [Fact]
    public void Wynik_zaokraglony_do_pelnych_zlotych()
    {
        // Podstawa 29 001 → 12% × 29 001 − 3 600 = 3 480,12 − 3 600 = −119,88 → 0
        Assert.Equal(0m, AnnualSettlementCalculator.ScaleTax(29_001m, P2025));

        // Podstawa 100 001 → 12% × 100 001 − 3 600 = 12 000,12 − 3 600 = 8 400,12 → 8 400
        Assert.Equal(8_400m, AnnualSettlementCalculator.ScaleTax(100_001m, P2025));
    }
}
