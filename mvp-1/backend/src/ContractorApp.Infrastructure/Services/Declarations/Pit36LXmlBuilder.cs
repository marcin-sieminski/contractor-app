using System.Xml.Linq;
using ContractorApp.Application.Features.Settlements;
using ContractorApp.Domain.Enums;

namespace ContractorApp.Infrastructure.Services.Declarations;

/// <summary>
/// PIT-36L(21) — podatek liniowy 19%. Mapowanie pozycji wg schemy
/// crd.gov.pl/wzor/2025/09/25/13874. Podmiot1 bez adresu (forma go nie zawiera).
/// </summary>
public class Pit36LXmlBuilder(DeclarationXsdValidator validator) : PitXmlBuilderBase(validator)
{
    public override TaxForm Form => TaxForm.Liniowy;
    protected override bool IncludesAddress => false;

    protected override XElement BuildPozycjeSzczegolowe(XNamespace tns, AnnualSettlementDto s)
    {
        var r = s.Result;
        var ipBox = r.PodstawaIpBox > 0m;
        var podstawaPozaIp = r.PodstawaOpodatkowania - r.PodstawaIpBox;

        return new XElement(tns + "PozycjeSzczegolowe",
            // E.1. Pozarolnicza działalność gospodarcza (z PIT/B)
            new XElement(tns + "P_28", Amount2(r.Przychod)),
            new XElement(tns + "P_29", Amount2(r.Koszty)),
            new XElement(tns + "P_30", Amount2(r.Dochod)),
            AmountIf(tns, "P_31", r.Strata, Amount2(r.Strata), r.Strata > 0m),
            new XElement(tns + "P_32", AmountWhole(r.ZaliczkiWplacone)),
            // F. Odliczenia od dochodu
            new XElement(tns + "P_40", Amount2(r.SkladkiSpoleczneOdliczone)),
            new XElement(tns + "P_41", Amount2(r.SkladkaZdrowotnaOdliczona)),
            new XElement(tns + "P_50", Amount2(podstawaPozaIp)),
            // G. Podstawa obliczenia podatku
            new XElement(tns + "P_67", Amount2(podstawaPozaIp)),
            new XElement(tns + "P_70", Amount2(podstawaPozaIp)),
            new XElement(tns + "P_72", Amount2(podstawaPozaIp)),
            new XElement(tns + "P_73", AmountWhole(podstawaPozaIp)),
            // H. Obliczenie podatku (19% od podstawy poza IP Box)
            new XElement(tns + "P_74", AmountWhole(r.PodatekPozaIpBox)),
            new XElement(tns + "P_78", Amount2(r.PodatekPozaIpBox)),
            // I. Odliczenia od podatku
            new XElement(tns + "P_80", Amount2(r.PodatekPozaIpBox)),
            // J. Obliczenie zobowiązania podatkowego
            new XElement(tns + "P_81", AmountWhole(r.PodatekNalezny)),
            new XElement(tns + "P_82", AmountWhole(r.ZaliczkiWplacone)),
            DueOrOverpaid(tns, "P_83", "P_84",
                Math.Max(0m, r.PodatekNalezny - r.ZaliczkiWplacone),
                Math.Max(0m, r.ZaliczkiWplacone - r.PodatekNalezny)),
            // K. Zaliczki zapłacone — suma wpłat podatnika
            new XElement(tns + "P_136", AmountWhole(r.ZaliczkiWplacone)),
            new XElement(tns + "P_137", AmountWhole(r.ZaliczkiWplacone)),
            // Podatek z załącznika PIT/IP (5%)
            AmountIf(tns, "P_143", r.PodatekIpBox, AmountWhole(r.PodatekIpBox), ipBox),
            // L. Podatek do zapłaty / nadpłata
            DueOrOverpaid(tns, "P_145", "P_146", r.DoZaplaty, r.Nadplata),
            // Informacje o załącznikach
            new XElement(tns + "P_178", 1),
            AmountIf(tns, "P_179", 1m, "1", ipBox));
    }

    protected override XElement? BuildZalaczniki(XNamespace tns, AnnualSettlementDto s)
    {
        var zalaczniki = new XElement(tns + "Zalaczniki",
            PitAttachments.PitB(DeclarationSchemaCatalog.PitB36XNamespace, s));
        if (s.Result.PodstawaIpBox > 0m)
            zalaczniki.Add(PitAttachments.PitIp(DeclarationSchemaCatalog.PitIp36XNamespace, s));
        return zalaczniki;
    }
}
