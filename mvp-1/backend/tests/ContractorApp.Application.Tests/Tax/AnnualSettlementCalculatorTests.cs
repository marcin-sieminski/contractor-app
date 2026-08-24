using ContractorApp.Application.Common.Tax;
using ContractorApp.Domain.Enums;
using Xunit;

namespace ContractorApp.Application.Tests.Tax;

/// <summary>
/// Złote scenariusze rozliczenia rocznego (stawki 2025), wartości policzone ręcznie
/// i zweryfikowane z kalkulatorami podatki.gov.pl. ZUS społeczny pełny 2025:
/// 1646,47 × 12 = 19 757,64.
/// </summary>
public class AnnualSettlementCalculatorTests
{
    private static readonly TaxYearParameters P2025 = TaxYearParametersProvider.ForYear(2025).Parameters;
    private const decimal SocialFullYear = 19_757.64m; // 12 × 1646,47

    private static SettlementCalculationInput Input(
        TaxForm form, decimal revenue, decimal costs = 0m,
        decimal social = 0m, decimal health = 0m, decimal prepayments = 0m,
        bool ipBox = false, decimal ipPercent = 0m, decimal nexus = 1m) =>
        new(2025, form, P2025, revenue, costs, social, health, prepayments, ipBox, ipPercent, nexus);

    // ── PIT-36L (liniowy) ───────────────────────────────────────────────────────

    [Fact]
    public void Liniowy_typowy_rok_z_limitem_skladki_zdrowotnej()
    {
        // dochód 270 000 − społeczne 19 757,64 = 250 242,36; zdrowotna 13 000 → limit 12 900;
        // podstawa 237 342; podatek 19% = 45 094,98 → 45 095.
        var r = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Liniowy, 300_000m, 30_000m, SocialFullYear, 13_000m, prepayments: 44_000m));

        Assert.Equal(270_000m, r.Dochod);
        Assert.Equal(19_757.64m, r.SkladkiSpoleczneOdliczone);
        Assert.Equal(12_900m, r.SkladkaZdrowotnaOdliczona); // ograniczona limitem
        Assert.Equal(237_342m, r.PodstawaOpodatkowania);
        Assert.Equal(45_095m, r.PodatekNalezny);
        Assert.Equal(1_095m, r.DoZaplaty);
        Assert.Equal(0m, r.Nadplata);
    }

    [Fact]
    public void Liniowy_zdrowotna_ponizej_limitu_odliczona_w_calosci()
    {
        // dochód 100 000 − społeczne 12 000 = 88 000 − zdrowotna 5 000 = 83 000;
        // podatek 19% = 15 770.
        var r = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Liniowy, 120_000m, 20_000m, 12_000m, 5_000m));

        Assert.Equal(5_000m, r.SkladkaZdrowotnaOdliczona);
        Assert.Equal(83_000m, r.PodstawaOpodatkowania);
        Assert.Equal(15_770m, r.PodatekNalezny);
    }

    [Fact]
    public void Liniowy_strata_daje_zerowy_podatek_i_nadplate_z_zaliczek()
    {
        var r = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Liniowy, 50_000m, 80_000m, 10_000m, 4_000m, prepayments: 1_000m));

        Assert.Equal(0m, r.Dochod);
        Assert.Equal(30_000m, r.Strata);
        Assert.Equal(0m, r.PodstawaOpodatkowania);
        Assert.Equal(0m, r.PodatekNalezny);
        Assert.Equal(0m, r.DoZaplaty);
        Assert.Equal(1_000m, r.Nadplata);
        Assert.Contains(r.Lines, l => l.BoxId == "STRATA" && l.Value == 30_000m);
    }

    // ── PIT-36 (skala) ──────────────────────────────────────────────────────────

    [Fact]
    public void Skala_ponizej_progu_z_kwota_zmniejszajaca()
    {
        // dochód 120 000 − społeczne 19 757,64 → podstawa 100 242;
        // podatek = 12% × 100 242 − 3600 = 8 429,04 → 8 429.
        var r = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Skala, 150_000m, 30_000m, SocialFullYear));

        Assert.Equal(100_242m, r.PodstawaOpodatkowania);
        Assert.Equal(8_429m, r.PodatekNalezny);
    }

    [Fact]
    public void Skala_niski_dochod_zerowy_podatek_przez_kwote_wolna()
    {
        // podstawa 29 000 → 12% × 29 000 − 3600 = −120 → 0.
        var r = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Skala, 40_000m, 5_000m, 6_000m));

        Assert.Equal(29_000m, r.PodstawaOpodatkowania);
        Assert.Equal(0m, r.PodatekNalezny);
    }

    [Fact]
    public void Skala_dokladnie_na_progu()
    {
        // podstawa 120 000 → 14 400 − 3600 = 10 800.
        var r = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Skala, 139_757.64m, 0m, SocialFullYear));

        Assert.Equal(120_000m, r.PodstawaOpodatkowania);
        Assert.Equal(10_800m, r.PodatekNalezny);
    }

    [Fact]
    public void Skala_powyzej_progu_pelna_formula_bez_bledu_przesunietego_progu()
    {
        // podstawa 200 000 → 10 800 + 32% × 80 000 = 36 400.
        // (Błędne odjęcie kwoty wolnej od podstawy dałoby 30 400 — pilnujemy formuły
        // z kwotą zmniejszającą, bez przesuniętego progu.)
        var r = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Skala, 219_757.64m, 0m, SocialFullYear));

        Assert.Equal(200_000m, r.PodstawaOpodatkowania);
        Assert.Equal(36_400m, r.PodatekNalezny);
    }

    [Fact]
    public void Skala_nie_odlicza_skladki_zdrowotnej()
    {
        var r = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Skala, 100_000m, 0m, 0m, health: 9_000m));

        Assert.Equal(0m, r.SkladkaZdrowotnaOdliczona);
        Assert.Equal(100_000m, r.PodstawaOpodatkowania);
    }

    // ── PIT-28 (ryczałt) ────────────────────────────────────────────────────────

    [Fact]
    public void Ryczalt_odlicza_spoleczne_i_polowe_zdrowotnej_ignoruje_koszty()
    {
        // 240 000 − 19 757,64 − 50% × 9 233,16 (= 4 616,58) = 215 625,78 → 215 626;
        // podatek 12% = 25 875,12 → 25 875; zaliczki 26 000 → nadpłata 125.
        var r = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Ryczalt, 240_000m, costs: 50_000m,
            social: SocialFullYear, health: 9_233.16m, prepayments: 26_000m));

        Assert.Equal(0m, r.Koszty); // ryczałt nie uwzględnia kosztów
        Assert.Equal(4_616.58m, r.SkladkaZdrowotnaOdliczona);
        Assert.Equal(215_626m, r.PodstawaOpodatkowania);
        Assert.Equal(25_875m, r.PodatekNalezny);
        Assert.Equal(0m, r.DoZaplaty);
        Assert.Equal(125m, r.Nadplata);
    }

    [Fact]
    public void Ryczalt_ignoruje_flage_ip_box()
    {
        var withIp = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Ryczalt, 240_000m, social: SocialFullYear, ipBox: true, ipPercent: 100m));
        var withoutIp = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Ryczalt, 240_000m, social: SocialFullYear));

        Assert.Equal(withoutIp.PodatekNalezny, withIp.PodatekNalezny);
        Assert.Equal(0m, withIp.PodstawaIpBox);
    }

    // ── IP Box ──────────────────────────────────────────────────────────────────

    [Fact]
    public void IpBox_100_procent_liniowy_caly_dochod_5_procent()
    {
        // podstawa jak w typowym liniowym: 237 342 → 5% = 11 867,1 → 11 867.
        var r = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Liniowy, 300_000m, 30_000m, SocialFullYear, 13_000m,
            ipBox: true, ipPercent: 100m));

        Assert.Equal(237_342m, r.PodstawaIpBox);
        Assert.Equal(11_867m, r.PodatekIpBox);
        Assert.Equal(0m, r.PodatekPozaIpBox);
        Assert.Equal(11_867m, r.PodatekNalezny);
    }

    [Fact]
    public void IpBox_50_procent_liniowy_dzieli_podstawe()
    {
        // podstawa 237 342: IP 118 671 → 5% = 5 933,55 → 5 934;
        // poza IP 118 671 → 19% = 22 547,49 → 22 547; razem 28 481.
        var r = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Liniowy, 300_000m, 30_000m, SocialFullYear, 13_000m,
            ipBox: true, ipPercent: 50m));

        Assert.Equal(118_671m, r.PodstawaIpBox);
        Assert.Equal(5_934m, r.PodatekIpBox);
        Assert.Equal(22_547m, r.PodatekPozaIpBox);
        Assert.Equal(28_481m, r.PodatekNalezny);
    }

    [Fact]
    public void IpBox_nexus_obniza_udzial_kwalifikowany()
    {
        // 100% × Nexus 0,5 = udział 0,5 → identycznie jak 50% z Nexus 1,0.
        var withNexus = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Liniowy, 300_000m, 30_000m, SocialFullYear, 13_000m,
            ipBox: true, ipPercent: 100m, nexus: 0.5m));
        var withPercent = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Liniowy, 300_000m, 30_000m, SocialFullYear, 13_000m,
            ipBox: true, ipPercent: 50m));

        Assert.Equal(withPercent.PodatekNalezny, withNexus.PodatekNalezny);
    }

    [Fact]
    public void IpBox_zero_procent_rownowazny_brakowi_ulgi()
    {
        var withZero = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Liniowy, 300_000m, 30_000m, SocialFullYear, 13_000m,
            ipBox: true, ipPercent: 0m));
        var without = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Liniowy, 300_000m, 30_000m, SocialFullYear, 13_000m));

        Assert.Equal(without.PodatekNalezny, withZero.PodatekNalezny);
    }

    // ── Zaokrąglenia (art. 63 §1 Ordynacji podatkowej) ─────────────────────────

    [Theory]
    [InlineData(100.49, 100)]
    [InlineData(100.50, 101)]
    [InlineData(0.49, 0)]
    [InlineData(0.50, 1)]
    public void Zaokraglenie_do_pelnych_zlotych(decimal value, decimal expected)
    {
        Assert.Equal(expected, AnnualSettlementCalculator.RoundPln(value));
    }

    [Fact]
    public void Podstawa_i_podatek_zaokraglane_do_pelnych_zlotych()
    {
        // dochód 1000,49 → podstawa 1000; podatek 19% = 190,00 → 190.
        var r = AnnualSettlementCalculator.Calculate(Input(TaxForm.Liniowy, 1_000.49m));

        Assert.Equal(1_000m, r.PodstawaOpodatkowania);
        Assert.Equal(190m, r.PodatekNalezny);
    }

    // ── Pozycje rozliczenia ────────────────────────────────────────────────────

    [Fact]
    public void Linie_zawieraja_komplet_pozycji_dla_liniowego()
    {
        var r = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Liniowy, 300_000m, 30_000m, SocialFullYear, 13_000m, prepayments: 44_000m));

        var boxIds = r.Lines.Select(l => l.BoxId).ToList();
        Assert.Equal(
            new[] { "PRZYCHOD", "KOSZTY", "DOCHOD", "SKLADKI_SPOLECZNE", "SKLADKA_ZDROWOTNA",
                    "PODSTAWA", "PODATEK", "ZALICZKI", "DO_ZAPLATY" },
            boxIds);
        Assert.All(r.Lines, l => Assert.False(string.IsNullOrWhiteSpace(l.Description)));
    }

    [Fact]
    public void Odliczenia_nie_przekraczaja_bazy()
    {
        // Społeczne 50 000 > dochód 20 000 → odliczone tylko 20 000, podstawa 0.
        var r = AnnualSettlementCalculator.Calculate(Input(
            TaxForm.Liniowy, 30_000m, 10_000m, social: 50_000m, health: 10_000m));

        Assert.Equal(20_000m, r.SkladkiSpoleczneOdliczone);
        Assert.Equal(0m, r.SkladkaZdrowotnaOdliczona);
        Assert.Equal(0m, r.PodstawaOpodatkowania);
        Assert.Equal(0m, r.PodatekNalezny);
    }
}
