using ContractorApp.Application.Common.Tax;
using ContractorApp.Application.Features.Settlements;
using ContractorApp.Domain.Enums;
using ContractorApp.Infrastructure.Services.Pdf;
using QuestPDF.Infrastructure;
using Xunit;

namespace ContractorApp.Application.Tests.Pdf;

public class SettlementPdfGeneratorTests
{
    static SettlementPdfGeneratorTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static AnnualSettlementDto Dto(TaxForm form, bool ipBox = false)
    {
        var (parameters, _) = TaxYearParametersProvider.ForYear(2025);
        var calc = AnnualSettlementCalculator.Calculate(new SettlementCalculationInput(
            2025, form, parameters,
            Revenue: 300_000m, Costs: form == TaxForm.Ryczalt ? 0m : 30_000m,
            ZusSocialPaid: 19_757.64m, ZusHealthPaid: 13_000m, TaxPrepaymentsPaid: 30_000m,
            IpBoxEnabled: ipBox, IpQualifyingPercent: 80m));

        return new AnnualSettlementDto(
            2025, SettlementForms.Key(form), SettlementForms.Label(form), SettlementForms.Code(form),
            "final", true, DateTimeOffset.UtcNow, true,
            new SettlementPrefillDto(300_000m, 30_000m, 19_757.64m, 13_000m, 30_000m),
            new SettlementAdjustmentsDto(null, null, 19_757.64m, 13_000m, 30_000m, ipBox, 80m, 1m),
            new TaxpayerDataDto(
                "5252248481", "Żaneta", "Łętowska-Świątek", new DateOnly(1985, 3, 14),
                "Prosta", "12", "3", "00-850", "Łódź", "1409",
                "łódzkie", "Łódź", "Łódź"),
            new SettlementResultDto(
                calc.Przychod, calc.Koszty, calc.Dochod, calc.Strata,
                calc.SkladkiSpoleczneOdliczone, calc.SkladkaZdrowotnaOdliczona,
                calc.PodstawaOpodatkowania, calc.PodstawaIpBox, calc.PodatekIpBox,
                calc.PodatekPozaIpBox, calc.PodatekNalezny, calc.ZaliczkiWplacone,
                calc.DoZaplaty, calc.Nadplata, calc.EfektywnaStawkaOdPrzychodu),
            calc.Lines.Select(l => new SettlementLineDto(l.BoxId, l.Label, l.Value, l.Description)).ToList(),
            calc.Notes.ToList(),
            ["Przykładowe ostrzeżenie o danych."]);
    }

    [Theory]
    [InlineData(TaxForm.Liniowy)]
    [InlineData(TaxForm.Skala)]
    [InlineData(TaxForm.Ryczalt)]
    public void Generuje_niepusty_pdf_dla_kazdej_formy(TaxForm form)
    {
        var pdf = new SettlementPdfGenerator().Generate(Dto(form));

        Assert.True(pdf.Length > 2_000, $"PDF ma tylko {pdf.Length} bajtów");
        Assert.Equal((byte)'%', pdf[0]); // nagłówek %PDF
        Assert.Equal((byte)'P', pdf[1]);
    }

    [Fact]
    public void Generuje_pdf_ze_scenariuszem_ip_box()
    {
        var pdf = new SettlementPdfGenerator().Generate(Dto(TaxForm.Liniowy, ipBox: true));

        Assert.True(pdf.Length > 2_000);
    }
}
