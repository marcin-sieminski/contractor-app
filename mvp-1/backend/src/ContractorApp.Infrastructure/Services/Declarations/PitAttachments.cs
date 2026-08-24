using System.Globalization;
using System.Xml.Linq;
using ContractorApp.Application.Features.Settlements;

namespace ContractorApp.Infrastructure.Services.Declarations;

/// <summary>
/// Załączniki PIT/B(22) i PIT/IP(5) — identyczna struktura dla PIT-36 (Z36) i PIT-36L (Z36X),
/// różnią się wyłącznie przestrzenią nazw.
/// </summary>
internal static class PitAttachments
{
    private static readonly XNamespace Etd = DeclarationSchemaCatalog.EtdNamespace;

    private static string A2(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);
    private static string AW(decimal v) => decimal.Truncate(v).ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Podmiot1 załącznika (tylko warianty Z36 dla PIT-36): typ etd:TIdentyfikatorOsobyFizycznej2 —
    /// element w przestrzeni załącznika, pola w przestrzeni DefinicjeTypy.
    /// </summary>
    private static XElement Podmiot1(XNamespace ns, AnnualSettlementDto s)
    {
        var t = s.Taxpayer;
        return new XElement(ns + "Podmiot1",
            new XElement(Etd + "NIP", new string((t.Nip ?? "").Where(char.IsDigit).ToArray())),
            new XElement(Etd + "ImiePierwsze", t.FirstName),
            new XElement(Etd + "Nazwisko", t.LastName),
            new XElement(Etd + "DataUrodzenia",
                t.BirthDate!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// PIT/B: dochód z działalności gospodarczej. Sekcja B = działalność na własne nazwisko,
    /// sekcja D = łącznie; przy IP Box w P_26 dochód kwalifikowany wyłączony z opodatkowania
    /// formą bazową (wykazywany w PIT/IP).
    /// </summary>
    public static XElement PitB(XNamespace ns, AnnualSettlementDto s, bool includePodmiot = false)
    {
        var r = s.Result;
        var ipBox = r.PodstawaIpBox > 0m;
        var isLoss = r.Strata > 0m;

        var pozycje = new XElement(ns + "PozycjeSzczegolowe",
            // B. Działalność gospodarcza na własne nazwisko (dochód XOR strata)
            new XElement(ns + "P_7", A2(r.Przychod)),
            new XElement(ns + "P_8", A2(r.Koszty)),
            new XElement(ns + (isLoss ? "P_10" : "P_9"), A2(isLoss ? r.Strata : r.Dochod)),
            // D. Łączne przychody, koszty, dochód / strata
            new XElement(ns + "P_22", A2(r.Przychod)),
            new XElement(ns + "P_23", A2(r.Koszty)),
            new XElement(ns + (isLoss ? "P_25" : "P_24"), A2(isLoss ? r.Strata : r.Dochod)));

        if (ipBox)
            pozycje.Add(
                // Dochód kwalifikowany IP wyłączony z formy bazowej (wykazany w PIT/IP)
                new XElement(ns + "P_26", A2(r.PodstawaIpBox)),
                new XElement(ns + "P_28", A2(Math.Max(0m, r.Dochod - r.PodstawaIpBox))));

        return new XElement(ns + "Zalacznik_PIT_B",
            new XElement(ns + "Naglowek",
                new XElement(ns + "KodFormularza", "PIT/B",
                    new XAttribute("kodSystemowy", "PIT/B (22)"),
                    new XAttribute("wersjaSchemy", "1-0E")),
                new XElement(ns + "WariantFormularza", 22)),
            includePodmiot ? Podmiot1(ns, s) : null,
            pozycje);
    }

    /// <summary>
    /// PIT/IP: dochód z kwalifikowanych praw własności intelektualnej (program komputerowy)
    /// opodatkowany stawką 5%. Kwoty wg uproszczenia kalkulatora: udział kwalifikowany
    /// stosowany do podstawy po odliczeniach.
    /// </summary>
    public static XElement PitIp(XNamespace ns, AnnualSettlementDto s, bool includePodmiot = false)
    {
        var r = s.Result;
        var share = r.PodstawaOpodatkowania > 0m
            ? r.PodstawaIpBox / r.PodstawaOpodatkowania
            : 0m;

        var pozycje = new XElement(ns + "PozycjeSzczegolowe",
            // Autorskie prawo do programu komputerowego — liczba praw
            new XElement(ns + "P_13", 1),
            new XElement(ns + "P_16", A2(Math.Round(r.Przychod * share, 2))),
            new XElement(ns + "P_17", A2(Math.Round(r.Koszty * share, 2))),
            new XElement(ns + "P_18", A2(r.PodstawaIpBox)),
            new XElement(ns + "P_19", A2(0m)),
            new XElement(ns + "P_20", A2(r.PodstawaIpBox)),
            new XElement(ns + "P_21", A2(0m)),
            new XElement(ns + "P_22", A2(0m)),
            new XElement(ns + "P_29", AW(r.PodstawaIpBox)),
            new XElement(ns + "P_35", AW(r.PodstawaIpBox)),
            new XElement(ns + "P_38", AW(r.PodstawaIpBox)),
            new XElement(ns + "P_40", AW(r.PodstawaIpBox)),
            new XElement(ns + "P_41", AW(r.PodatekIpBox)),
            new XElement(ns + "P_44", AW(r.PodatekIpBox)));

        return new XElement(ns + "Zalacznik_PIT_IP",
            new XElement(ns + "Naglowek",
                new XElement(ns + "KodFormularza", "PIT/IP",
                    new XAttribute("kodSystemowy", "PIT/IP (5)"),
                    new XAttribute("wersjaSchemy", "1-0E")),
                new XElement(ns + "WariantFormularza", 5)),
            includePodmiot ? Podmiot1(ns, s) : null,
            pozycje);
    }
}
