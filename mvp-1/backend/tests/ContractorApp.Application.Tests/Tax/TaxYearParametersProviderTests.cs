using ContractorApp.Application.Common.Tax;
using Xunit;

namespace ContractorApp.Application.Tests.Tax;

public class TaxYearParametersProviderTests
{
    [Theory]
    [InlineData(2025)]
    [InlineData(2026)]
    public void Znany_rok_zwraca_dokladna_tabele(int year)
    {
        var (parameters, isExact) = TaxYearParametersProvider.ForYear(year);

        Assert.True(isExact);
        Assert.Equal(year, parameters.Year);
    }

    [Fact]
    public void Rok_przyszly_bez_tabeli_uzywa_najblizszej_i_sygnalizuje_przyblizenie()
    {
        var (parameters, isExact) = TaxYearParametersProvider.ForYear(2030);

        Assert.False(isExact);
        Assert.Equal(2026, parameters.Year);
    }

    [Fact]
    public void Rok_przeszly_bez_tabeli_uzywa_najblizszej()
    {
        var (parameters, isExact) = TaxYearParametersProvider.ForYear(2020);

        Assert.False(isExact);
        Assert.Equal(2025, parameters.Year);
    }

    [Fact]
    public void Tabela_2026_jest_spojna_ze_stalymi_PolishTaxCalculator()
    {
        var (p, _) = TaxYearParametersProvider.ForYear(2026);

        Assert.Equal(PolishTaxCalculator.ZusSocialFull, p.ZusSocialFullMonthly);
        Assert.Equal(PolishTaxCalculator.HealthRyczaltMid, p.HealthRyczaltMidMonthly);
        Assert.Equal(PolishTaxCalculator.LinearPitRate, p.LinearPitRate);
        Assert.Equal(PolishTaxCalculator.TaxFreeAmount, p.TaxFreeAmount);
    }
}
