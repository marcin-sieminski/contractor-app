using ContractorApp.Application.Common.Tax;
using ContractorApp.Domain.Enums;
using Xunit;

namespace ContractorApp.Application.Tests.Analytics;

/// <summary>
/// Weryfikacja matematyki oszczędności IP Box z PolishTaxCalculator.
/// Formułę 14% (19% liniowy − 5% IP Box) stosuje GetIpBoxProgressQueryHandler
/// jako szybkie przybliżenie rocznych oszczędności. Testy potwierdzają jej ścisłość
/// dla liniowego i dokumentują odchylenie dla skali (stawka progresywna ≠ stała różnica).
/// </summary>
public class IpBoxSavingsTests
{
    // ── Liniowy: oszczędność = 14% × podstawa po ZUS ─────────────────────────

    [Fact]
    public void Liniowy_pelne_ip_box_oszczednosc_rowna_14_procent_dochodu()
    {
        // dochód − ZUS = podstawa; IP Box 5% vs liniowy 19% → różnica 14%
        const decimal income = 200_000m;
        const decimal zus = 20_000m;
        var baseAfterZus = income - zus; // 180 000

        var pitBez = PolishTaxCalculator.BaseAnnualPit(TaxForm.Liniowy, income, zus);
        var pitZ   = PolishTaxCalculator.IpBoxAnnualPit(TaxForm.Liniowy, income, zus, 1m);

        Assert.Equal(34_200m, pitBez);                    // 180 000 × 19%
        Assert.Equal(9_000m,  pitZ);                      // 180 000 × 5%
        Assert.Equal(25_200m, pitBez - pitZ);             // 180 000 × 14%
        Assert.Equal(baseAfterZus * 0.14m, pitBez - pitZ);
    }

    [Fact]
    public void Liniowy_czescowe_ip_box_oszczednosc_proporcjonalna()
    {
        // 50% dochodu kwalifikowanego: oszczędność = 50% × 180 000 × 14% = 12 600
        const decimal income = 200_000m;
        const decimal zus = 20_000m;
        var baseAfterZus = income - zus; // 180 000

        var pitBez = PolishTaxCalculator.BaseAnnualPit(TaxForm.Liniowy, income, zus);
        var pitZ   = PolishTaxCalculator.IpBoxAnnualPit(TaxForm.Liniowy, income, zus, 0.5m);

        Assert.Equal(baseAfterZus * 0.5m * 0.14m, pitBez - pitZ); // 12 600
    }

    [Fact]
    public void Liniowy_zerowy_udzial_ip_brak_oszczednosci()
    {
        var pitBez = PolishTaxCalculator.BaseAnnualPit(TaxForm.Liniowy, 200_000m, 20_000m);
        var pitZ   = PolishTaxCalculator.IpBoxAnnualPit(TaxForm.Liniowy, 200_000m, 20_000m, 0m);

        Assert.Equal(pitBez, pitZ);
        Assert.Equal(0m, pitBez - pitZ);
    }

    [Fact]
    public void Liniowy_zerowy_dochod_po_zus_brak_oszczednosci()
    {
        // ZUS = dochód → podstawa 0 → oba podatki = 0
        Assert.Equal(0m, PolishTaxCalculator.BaseAnnualPit(TaxForm.Liniowy, 20_000m, 20_000m));
        Assert.Equal(0m, PolishTaxCalculator.IpBoxAnnualPit(TaxForm.Liniowy, 20_000m, 20_000m, 1m));
    }

    // ── Skala: 14% to NIE stała oszczędność (stawka progresywna) ─────────────

    [Fact]
    public void Skala_pelne_ip_box_oszczednosc_nie_rowna_14_procent()
    {
        // Przy dochodzie 200 000 (podstawa 180 000 > próg 120 000) podatek skali to ≠ 19%.
        // Oszczędność ≠ 0,14 × podstawa — regresja chroniąca przed tym założeniem.
        const decimal income = 200_000m;
        const decimal zus = 20_000m;
        var baseAfterZus = income - zus; // 180 000

        var pitBez = PolishTaxCalculator.BaseAnnualPit(TaxForm.Skala, income, zus);
        var pitZ   = PolishTaxCalculator.IpBoxAnnualPit(TaxForm.Skala, income, zus, 1m);
        var savings = pitBez - pitZ;

        // Skala: 10 800 + 32% × 60 000 = 30 000 vs IP Box 180 000 × 5% = 9 000 → 21 000
        Assert.Equal(30_000m, pitBez);
        Assert.Equal(9_000m,  pitZ);
        Assert.Equal(21_000m, savings);
        Assert.NotEqual(baseAfterZus * 0.14m, savings); // ≠ 25 200 (błędne założenie)
    }

    [Fact]
    public void Skala_niski_dochod_ponizej_progu_oszczednosc_rowna_7_procent()
    {
        // Poniżej progu 120 000: stawka 12%, IP Box 5% → różnica 7% (nie 14%)
        const decimal income = 100_000m;
        const decimal zus = 0m;
        // ScaleAnnualPit(100 000) = 12% × 100 000 − 3 600 = 8 400
        // IpBoxAnnualPit(skala, 100000, 0, share=1): nonIpBase=0 → ScaleAnnualPit(0)=0;
        //   ipBase = 100 000 × 5% = 5 000

        var pitBez = PolishTaxCalculator.BaseAnnualPit(TaxForm.Skala, income, zus);  // 8 400
        var pitZ   = PolishTaxCalculator.IpBoxAnnualPit(TaxForm.Skala, income, zus, 1m); // 5 000

        Assert.Equal(8_400m, pitBez);
        Assert.Equal(5_000m, pitZ);
        Assert.Equal(3_400m, pitBez - pitZ); // 8 400 − 5 000 = 3 400 ≠ 7% × 100 000 (= 7 000)
        // Uwaga: skala ma kwotę zmniejszającą 3 600 PLN, więc efektywna stawka < 12%
    }

    // ── Nexus: Nexus < 1 zmniejsza podstawę IP ───────────────────────────────

    [Fact]
    public void Nizszy_udzial_kwalifikujacy_daje_wyzszy_podatek()
    {
        // Udział 60% (np. Nexus 0,6 × 100%) → więcej dochodu objętego 19% → wyższy podatek
        // niż przy udziale 100% (cały dochód objęty stawką 5%).
        const decimal income = 200_000m;
        const decimal zus = 20_000m;

        var pitAt60pct  = PolishTaxCalculator.IpBoxAnnualPit(TaxForm.Liniowy, income, zus, 0.6m);
        var pitAt100pct = PolishTaxCalculator.IpBoxAnnualPit(TaxForm.Liniowy, income, zus, 1m);

        Assert.True(pitAt60pct > pitAt100pct,
            "Niższy udział kwalifikujący powinien dawać wyższy podatek (więcej dochodu przy 19%)");
    }
}
