using ContractorApp.Application.Common.Tax;
using ContractorApp.Application.Features.Settlements;
using ContractorApp.Domain.Enums;
using ContractorApp.Infrastructure.Services.Declarations;
using Xunit;

namespace ContractorApp.Application.Tests.Declarations;

/// <summary>
/// Buduje XML każdej deklaracji ze złotego scenariusza i waliduje względem
/// osadzonych oficjalnych schem XSD MF (crd.gov.pl) — bez dostępu do sieci.
/// </summary>
public class DeclarationXmlBuilderTests
{
    private static readonly DeclarationXsdValidator Validator = new();

    private static AnnualSettlementDto Dto(TaxForm form, bool ipBox = false)
    {
        var (parameters, _) = TaxYearParametersProvider.ForYear(2025);
        var calc = AnnualSettlementCalculator.Calculate(new SettlementCalculationInput(
            2025, form, parameters,
            Revenue: 300_000m, Costs: form == TaxForm.Ryczalt ? 0m : 30_000m,
            ZusSocialPaid: 19_757.64m, ZusHealthPaid: 13_000m,
            TaxPrepaymentsPaid: 30_000m,
            IpBoxEnabled: ipBox, IpQualifyingPercent: 80m, NexusCoefficient: 1m));

        return new AnnualSettlementDto(
            2025, SettlementForms.Key(form), SettlementForms.Label(form), SettlementForms.Code(form),
            "draft", true, null, true,
            new SettlementPrefillDto(300_000m, 30_000m, 19_757.64m, 13_000m, 30_000m),
            new SettlementAdjustmentsDto(null, null, 19_757.64m, 13_000m, 30_000m, ipBox, 80m, 1m),
            new TaxpayerDataDto(
                "5252248481", "Jan", "Kowalski", new DateOnly(1985, 3, 14),
                "Prosta", "12", "3", "00-850", "Warszawa", "1409",
                "mazowieckie", "Warszawa", "Warszawa"),
            new SettlementResultDto(
                calc.Przychod, calc.Koszty, calc.Dochod, calc.Strata,
                calc.SkladkiSpoleczneOdliczone, calc.SkladkaZdrowotnaOdliczona,
                calc.PodstawaOpodatkowania, calc.PodstawaIpBox, calc.PodatekIpBox,
                calc.PodatekPozaIpBox, calc.PodatekNalezny, calc.ZaliczkiWplacone,
                calc.DoZaplaty, calc.Nadplata, calc.EfektywnaStawkaOdPrzychodu),
            calc.Lines.Select(l => new SettlementLineDto(l.BoxId, l.Label, l.Value, l.Description)).ToList(),
            calc.Notes.ToList(), []);
    }

    [Fact]
    public void Pit36L_buduje_xml_zgodny_ze_schema()
    {
        var xml = new Pit36LXmlBuilder(Validator).Build(Dto(TaxForm.Liniowy));

        Assert.Equal("PIT-36L", xml.FormCode);
        Assert.Contains("PIT36L (21)", xml.Content);
        Assert.Contains("Zalacznik_PIT_B", xml.Content);
        Assert.StartsWith("PIT-36L_2025_5252248481", xml.FileName);
    }

    [Fact]
    public void Pit36L_z_ip_box_dolacza_pit_ip()
    {
        var xml = new Pit36LXmlBuilder(Validator).Build(Dto(TaxForm.Liniowy, ipBox: true));

        Assert.Contains("Zalacznik_PIT_IP", xml.Content);
        Assert.Contains("P_143", xml.Content);
    }

    [Fact]
    public void Pit36_buduje_xml_zgodny_ze_schema()
    {
        var xml = new Pit36XmlBuilder(Validator).Build(Dto(TaxForm.Skala));

        Assert.Equal("PIT-36", xml.FormCode);
        Assert.Contains("PIT-36 (32)", xml.Content);
        Assert.Contains("Zalacznik_PIT_B", xml.Content);
    }

    [Fact]
    public void Pit36_z_ip_box_dolacza_pit_ip()
    {
        var xml = new Pit36XmlBuilder(Validator).Build(Dto(TaxForm.Skala, ipBox: true));

        Assert.Contains("Zalacznik_PIT_IP", xml.Content);
    }

    [Fact]
    public void Pit28_buduje_xml_zgodny_ze_schema()
    {
        var xml = new Pit28XmlBuilder(Validator).Build(Dto(TaxForm.Ryczalt));

        Assert.Equal("PIT-28", xml.FormCode);
        Assert.Contains("PIT-28 (27)", xml.Content);
        Assert.Contains("P_237", xml.Content); // ryczałt do zapłaty
    }

    [Fact]
    public void Brak_danych_podatnika_daje_czytelny_blad()
    {
        var dto = Dto(TaxForm.Liniowy) with
        {
            Taxpayer = new TaxpayerDataDto(null, null, null, null, null, null, null, null, null, null, null, null, null)
        };

        var ex = Assert.Throws<ContractorApp.Domain.Exceptions.DomainException>(
            () => new Pit36LXmlBuilder(Validator).Build(dto));
        Assert.Contains("dane podatnika", ex.Message);
    }

    [Fact]
    public void Nieobslugiwany_rok_daje_czytelny_blad()
    {
        var dto = Dto(TaxForm.Liniowy) with { Year = 2030 };

        var ex = Assert.Throws<ContractorApp.Domain.Exceptions.DomainException>(
            () => new Pit36LXmlBuilder(Validator).Build(dto));
        Assert.Contains("2030", ex.Message);
    }
}
