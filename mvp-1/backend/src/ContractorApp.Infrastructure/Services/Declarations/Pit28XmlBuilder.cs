using System.Xml.Linq;
using ContractorApp.Application.Features.Settlements;
using ContractorApp.Domain.Enums;

namespace ContractorApp.Infrastructure.Services.Declarations;

/// <summary>
/// PIT-28(27) — ryczałt od przychodów ewidencjonowanych, usługi IT 12%
/// (działalność na własne nazwisko). Mapowanie wg schemy crd.gov.pl/wzor/2025/10/10/13916.
/// </summary>
public class Pit28XmlBuilder(DeclarationXsdValidator validator) : PitXmlBuilderBase(validator)
{
    public override TaxForm Form => TaxForm.Ryczalt;

    protected override XElement BuildPozycjeSzczegolowe(XNamespace tns, AnnualSettlementDto s)
    {
        var r = s.Result;
        var odliczenia = r.SkladkiSpoleczneOdliczone + r.SkladkaZdrowotnaOdliczona;
        var przychodPoOdliczeniach = Math.Max(0m, r.Przychod - odliczenia);

        return new XElement(tns + "PozycjeSzczegolowe",
            // C.1. Przychody z działalności prowadzonej na własne nazwisko — stawka 12%
            new XElement(tns + "P_30", Amount2(r.Przychod)),
            new XElement(tns + "P_35", Amount2(r.Przychod)),
            // C.5. Razem przychody — stawka 12% i ogółem
            new XElement(tns + "P_57", Amount2(r.Przychod)),
            new XElement(tns + "P_63", Amount2(r.Przychod)),
            // D. Udział procentowy przychodów wg stawek (całość na 12%)
            new XElement(tns + "P_69", Percent(100m)),
            new XElement(tns + "P_74", Percent(100m)),
            // E.1. Odliczenia od przychodów: składki społeczne
            new XElement(tns + "P_98", Amount2(r.SkladkiSpoleczneOdliczone)),
            new XElement(tns + "P_100", Amount2(Math.Max(0m, r.Przychod - r.SkladkiSpoleczneOdliczone))),
            // E.4. Składki na ubezpieczenie zdrowotne — 50% zapłaconych
            new XElement(tns + "P_105", Amount2(r.SkladkaZdrowotnaOdliczona)),
            new XElement(tns + "P_107", Amount2(r.SkladkaZdrowotnaOdliczona)),
            // F. Wydatki odliczane od przychodów wg stawek (całość na 12%)
            new XElement(tns + "P_114", Amount2(r.SkladkiSpoleczneOdliczone)),
            new XElement(tns + "P_125", Amount2(r.SkladkaZdrowotnaOdliczona)),
            // G. Przychody po odliczeniach — stawka 12% i łącznie
            new XElement(tns + "P_137", Amount2(przychodPoOdliczeniach)),
            new XElement(tns + "P_144", Amount2(przychodPoOdliczeniach)),
            new XElement(tns + "P_183", Amount2(przychodPoOdliczeniach)),
            // H. Podstawa obliczenia ryczałtu — stawka 12%
            new XElement(tns + "P_202", AmountWhole(r.PodstawaOpodatkowania)),
            // I. Ryczałt wg stawek
            new XElement(tns + "P_217", AmountWhole(r.PodatekNalezny)),
            new XElement(tns + "P_224", AmountWhole(r.PodatekNalezny)),
            new XElement(tns + "P_225", Amount2(0m)),
            new XElement(tns + "P_228", Amount2(r.PodatekNalezny)),
            new XElement(tns + "P_230", Amount2(r.PodatekNalezny)),
            new XElement(tns + "P_232", AmountWhole(r.PodatekNalezny)),
            // K. Wpłacony ryczałt; ryczałt do zapłaty / nadpłata
            new XElement(tns + "P_236", AmountWhole(r.ZaliczkiWplacone)),
            DueOrOverpaid(tns, "P_237", "P_238", r.DoZaplaty, r.Nadplata));
    }

    // PIT-28 w naszym zakresie nie wymaga załączników (PIT-28/B dotyczy spółek).
    protected override XElement? BuildZalaczniki(XNamespace tns, AnnualSettlementDto s) => null;
}
