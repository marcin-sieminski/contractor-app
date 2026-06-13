using System.Xml.Linq;
using ContractorApp.Application.Features.Settlements;
using ContractorApp.Domain.Enums;

namespace ContractorApp.Infrastructure.Services.Declarations;

/// <summary>
/// PIT-36(32) — skala podatkowa, rozliczenie indywidualne, wyłącznie dochody z JDG.
/// Mapowanie pozycji wg schemy crd.gov.pl/wzor/2025/09/24/13871.
/// </summary>
public class Pit36XmlBuilder(DeclarationXsdValidator validator) : PitXmlBuilderBase(validator)
{
    public override TaxForm Form => TaxForm.Skala;

    protected override XElement BuildPozycjeSzczegolowe(XNamespace tns, AnnualSettlementDto s)
    {
        var r = s.Result;
        var ipBox = r.PodstawaIpBox > 0m;
        var podstawaPozaIp = r.PodstawaOpodatkowania - r.PodstawaIpBox;

        return new XElement(tns + "PozycjeSzczegolowe",
            // Wybór sposobu opodatkowania: indywidualnie
            new XElement(tns + "P_6", 1),
            // E.1. wiersz 3: Pozarolnicza działalność gospodarcza
            new XElement(tns + "P_87", Amount2(r.Przychod)),
            new XElement(tns + "P_88", Amount2(r.Koszty)),
            new XElement(tns + "P_89", Amount2(r.Dochod)),
            AmountIf(tns, "P_90", r.Strata, Amount2(r.Strata), r.Strata > 0m),
            new XElement(tns + "P_91", AmountWhole(r.ZaliczkiWplacone)),
            // E.1. wiersz Razem
            new XElement(tns + "P_124", Amount2(r.Przychod)),
            new XElement(tns + "P_125", Amount2(r.Koszty)),
            new XElement(tns + "P_126", Amount2(r.Dochod)),
            AmountIf(tns, "P_127", r.Strata, Amount2(r.Strata), r.Strata > 0m),
            new XElement(tns + "P_128", AmountWhole(r.ZaliczkiWplacone)),
            // F. Odliczenia: składki na ubezpieczenia społeczne (zdrowotna nieodliczalna na skali)
            new XElement(tns + "P_208", Amount2(r.SkladkiSpoleczneOdliczone)),
            new XElement(tns + "P_210", Amount2(podstawaPozaIp)),
            new XElement(tns + "P_232", Amount2(podstawaPozaIp)),
            // Podstawa obliczenia podatku
            new XElement(tns + "P_280", Amount2(podstawaPozaIp)),
            new XElement(tns + "P_292", Amount2(podstawaPozaIp)),
            new XElement(tns + "P_295", AmountWhole(podstawaPozaIp)),
            // Obliczenie podatku wg skali (z kwotą zmniejszającą)
            new XElement(tns + "P_296", AmountWhole(r.PodatekPozaIpBox)),
            new XElement(tns + "P_301", Amount2(r.PodatekPozaIpBox)),
            new XElement(tns + "P_304", Amount2(r.PodatekPozaIpBox)),
            new XElement(tns + "P_306", AmountWhole(r.PodatekNalezny)),
            new XElement(tns + "P_307", AmountWhole(r.ZaliczkiWplacone)),
            DueOrOverpaid(tns, "P_308", "P_309",
                Math.Max(0m, r.PodatekNalezny - r.ZaliczkiWplacone),
                Math.Max(0m, r.ZaliczkiWplacone - r.PodatekNalezny)),
            // Zaliczka zapłacona łącznie
            new XElement(tns + "P_361", AmountWhole(r.ZaliczkiWplacone)),
            // Podatek z załącznika PIT/IP
            AmountIf(tns, "P_429", r.PodatekIpBox, AmountWhole(r.PodatekIpBox), ipBox),
            // Podatek do zapłaty / nadpłata
            DueOrOverpaid(tns, "P_433", "P_435", r.DoZaplaty, r.Nadplata),
            // Informacje o załącznikach
            new XElement(tns + "P_516", 1),
            AmountIf(tns, "P_519", 1m, "1", ipBox));
    }

    // Wymagany przez schemę PIT-36(32); dotyczy oświadczenia z art. 6 ust. 2a ustawy
    // (weryfikowane przez podatnika przy wczytaniu do eFormularza).
    protected override XElement BuildOswiadczenie(XNamespace tns) =>
        new(tns + "Oswiadczenie", 1);

    protected override bool PouczeniaBeforeZalaczniki => true;

    protected override XElement? BuildZalaczniki(XNamespace tns, AnnualSettlementDto s)
    {
        var zalaczniki = new XElement(tns + "Zalaczniki",
            PitAttachments.PitB(DeclarationSchemaCatalog.PitB36Namespace, s, includePodmiot: true));
        if (s.Result.PodstawaIpBox > 0m)
            zalaczniki.Add(PitAttachments.PitIp(DeclarationSchemaCatalog.PitIp36Namespace, s, includePodmiot: true));
        return zalaczniki;
    }
}
