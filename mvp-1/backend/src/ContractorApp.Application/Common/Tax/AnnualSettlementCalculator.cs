using ContractorApp.Domain.Enums;

namespace ContractorApp.Application.Common.Tax;

/// <summary>Wejście kalkulatora rozliczenia rocznego — w pełni zmaterializowane, bez I/O.</summary>
/// <param name="Revenue">Roczny przychód netto w PLN (po przeliczeniu walut).</param>
/// <param name="Costs">Roczne koszty uzyskania w PLN (ryczałt je ignoruje).</param>
/// <param name="ZusSocialPaid">Składki społeczne zapłacone w roku (metoda kasowa).</param>
/// <param name="ZusHealthPaid">Składka zdrowotna zapłacona w roku (metoda kasowa).</param>
/// <param name="TaxPrepaymentsPaid">Zaliczki na PIT / ryczałt wpłacone w trakcie roku.</param>
/// <param name="IpQualifyingPercent">Udział dochodu kwalifikowanego IP w dochodzie ogółem (0–100).</param>
/// <param name="NexusCoefficient">Współczynnik Nexus (0–1); solo-kontraktor bez zakupu IP → 1,0.</param>
public sealed record SettlementCalculationInput(
    int Year,
    TaxForm Form,
    TaxYearParameters Parameters,
    decimal Revenue,
    decimal Costs,
    decimal ZusSocialPaid,
    decimal ZusHealthPaid,
    decimal TaxPrepaymentsPaid,
    bool IpBoxEnabled = false,
    decimal IpQualifyingPercent = 0m,
    decimal NexusCoefficient = 1m);

/// <summary>
/// Pojedyncza pozycja rozliczenia: stabilny klucz (mapowany na pole P_xx w XML),
/// polska etykieta i opis umiejscowienia w formularzu — wspólne źródło dla UI, XML i PDF.
/// </summary>
public sealed record SettlementLine(string BoxId, string Label, decimal Value, string? Description = null);

/// <summary>Wynik rozliczenia rocznego. Kwoty podstawy i podatku w pełnych złotych.</summary>
public sealed record SettlementCalculation(
    int Year,
    TaxForm Form,
    decimal Przychod,
    decimal Koszty,
    decimal Dochod,
    decimal Strata,
    decimal SkladkiSpoleczneOdliczone,
    decimal SkladkaZdrowotnaOdliczona,
    decimal PodstawaOpodatkowania,
    decimal PodstawaIpBox,
    decimal PodatekIpBox,
    decimal PodatekPozaIpBox,
    decimal PodatekNalezny,
    decimal ZaliczkiWplacone,
    decimal DoZaplaty,
    decimal Nadplata,
    decimal EfektywnaStawkaOdPrzychodu,
    IReadOnlyList<SettlementLine> Lines,
    IReadOnlyList<string> Notes);

/// <summary>
/// Czysty kalkulator rocznego zeznania PIT dla JDG: PIT-36 (skala), PIT-36L (liniowy),
/// PIT-28 (ryczałt 12% IT), z opcjonalnym IP Box (5%) dla liniowego i skali.
/// Zaokrąglenia podstawy i podatku do pełnych złotych (art. 63 §1 Ordynacji podatkowej)
/// wykonywane wyłącznie tutaj — nigdy w builderach XML/PDF.
/// Uwaga: świadomie NIE używa <see cref="PolishTaxCalculator.ScaleAnnualPit"/> —
/// ta sama formuła z kwotą zmniejszającą, ale tamta liczy stałymi roku bieżącego,
/// a rozliczenie za rok N wymaga parametrów roku N (<see cref="TaxYearParameters"/>).
/// </summary>
public static class AnnualSettlementCalculator
{
    public static SettlementCalculation Calculate(SettlementCalculationInput input)
    {
        var p = input.Parameters;
        var notes = new List<string>();

        var revenue = Math.Max(0m, input.Revenue);
        var costs = input.Form == TaxForm.Ryczalt ? 0m : Math.Max(0m, input.Costs);
        var socialPaid = Math.Max(0m, input.ZusSocialPaid);
        var healthPaid = Math.Max(0m, input.ZusHealthPaid);
        var prepayments = Math.Max(0m, input.TaxPrepaymentsPaid);

        var dochod = Math.Max(0m, revenue - costs);
        var strata = Math.Max(0m, costs - revenue);

        // Baza do odliczeń: ryczałt odlicza od przychodu, formy dochodowe od dochodu.
        var deductionBase = input.Form == TaxForm.Ryczalt ? revenue : dochod;
        var socialDeducted = Math.Min(socialPaid, deductionBase);
        var afterSocial = deductionBase - socialDeducted;

        var healthDeducted = input.Form switch
        {
            // Liniowy: zapłacona zdrowotna odliczana od dochodu do rocznego limitu.
            TaxForm.Liniowy => Math.Min(Math.Min(healthPaid, p.LinearHealthDeductionCap), afterSocial),
            // Ryczałt: 50% zapłaconej zdrowotnej odliczane od przychodu.
            TaxForm.Ryczalt => Math.Min(healthPaid * p.RyczaltHealthDeductibleShare, afterSocial),
            // Skala: brak odliczenia składki zdrowotnej.
            _ => 0m
        };

        var podstawa = RoundPln(Math.Max(0m, afterSocial - healthDeducted));

        // Udział IP Box: % dochodu kwalifikowanego × Nexus, tylko liniowy/skala.
        var ipBoxApplies = input.IpBoxEnabled && input.Form != TaxForm.Ryczalt;
        var ipShare = ipBoxApplies
            ? Math.Clamp(input.IpQualifyingPercent / 100m, 0m, 1m) * Math.Clamp(input.NexusCoefficient, 0m, 1m)
            : 0m;

        var podstawaIp = RoundPln(podstawa * ipShare);
        var podstawaPozaIp = podstawa - podstawaIp;

        var podatekIp = RoundPln(podstawaIp * p.IpBoxRate);
        var podatekPozaIp = input.Form switch
        {
            TaxForm.Ryczalt => RoundPln(podstawa * p.RyczaltItRate),
            TaxForm.Liniowy => RoundPln(podstawaPozaIp * p.LinearPitRate),
            _ => ScaleTax(podstawaPozaIp, p)
        };
        var podatek = ipBoxApplies ? podatekIp + podatekPozaIp : podatekPozaIp;

        var roznica = podatek - prepayments;
        var doZaplaty = RoundPln(Math.Max(0m, roznica));
        var nadplata = Math.Max(0m, Math.Round(-roznica, 2));

        // ── Noty założeń ────────────────────────────────────────────────────────
        notes.Add("Składki ZUS w wysokości faktycznie zapłaconej w roku podatkowym (metoda kasowa) — "
                  + "zweryfikuj kwoty z potwierdzeniami przelewów do ZUS.");
        if (input.Form == TaxForm.Liniowy && healthPaid > p.LinearHealthDeductionCap)
            notes.Add($"Składka zdrowotna odliczona do limitu {p.LinearHealthDeductionCap:N0} zł "
                      + $"(zapłacono {healthPaid:N2} zł).");
        if (input.Form == TaxForm.Ryczalt)
            notes.Add("Ryczałt: odliczeniu od przychodu podlega 50% zapłaconej składki zdrowotnej; "
                      + "koszty uzyskania przychodu nie są uwzględniane.");
        if (input.Form == TaxForm.Skala)
            notes.Add("Skala: składka zdrowotna nie podlega odliczeniu; podatek z kwotą zmniejszającą "
                      + $"{p.TaxReducingAmount:N0} zł (kwota wolna {p.TaxFreeAmount:N0} zł).");
        if (ipBoxApplies)
            notes.Add($"IP Box: 5% od {input.IpQualifyingPercent:N0}% podstawy (Nexus {input.NexusCoefficient:0.0#}); "
                      + "odliczenia składek przypisane do dochodu ogółem przed podziałem (uproszczenie). "
                      + "Wymaga odrębnej ewidencji IP i załącznika PIT/IP.");
        if (strata > 0m)
            notes.Add("Koszty przewyższają przychód — wykazano stratę; podatek 0 zł, "
                      + "wpłacone zaliczki stanowią nadpłatę.");
        notes.Add("Rozliczenie obejmuje wyłącznie dochody z JDG. Nie uwzględnia: wspólnego rozliczenia "
                  + "małżonków, innych źródeł przychodu, ulg z PIT/O, daniny solidarnościowej "
                  + "ani rocznego rozliczenia składki zdrowotnej z ZUS.");

        var lines = BuildLines(input.Form, revenue, costs, dochod, strata, socialDeducted,
            healthDeducted, podstawa, podstawaIp, podatekIp, podatekPozaIp, podatek,
            prepayments, doZaplaty, nadplata, ipBoxApplies);

        return new SettlementCalculation(
            input.Year, input.Form,
            Math.Round(revenue, 2), Math.Round(costs, 2), Math.Round(dochod, 2), Math.Round(strata, 2),
            Math.Round(socialDeducted, 2), Math.Round(healthDeducted, 2),
            podstawa, podstawaIp, podatekIp, podatekPozaIp, podatek,
            Math.Round(prepayments, 2), doZaplaty, nadplata,
            revenue > 0m ? Math.Round(podatek / revenue * 100m, 1) : 0m,
            lines, notes);
    }

    /// <summary>
    /// Podatek wg skali od zaokrąglonej podstawy: 12%·podstawa − kwota zmniejszająca;
    /// powyżej progu: 12%·próg − kwota zmniejszająca + 32%·nadwyżki. Nie mniej niż 0.
    /// </summary>
    public static decimal ScaleTax(decimal podstawa, TaxYearParameters p)
    {
        if (podstawa <= 0m) return 0m;
        var tax = podstawa <= p.ProgressiveThreshold
            ? podstawa * p.ScaleLowerRate - p.TaxReducingAmount
            : p.ProgressiveThreshold * p.ScaleLowerRate - p.TaxReducingAmount
              + (podstawa - p.ProgressiveThreshold) * p.ScaleUpperRate;
        return RoundPln(Math.Max(0m, tax));
    }

    /// <summary>Zaokrąglenie do pełnych złotych: końcówki &lt; 50 gr w dół, ≥ 50 gr w górę.</summary>
    public static decimal RoundPln(decimal value) =>
        Math.Round(value, 0, MidpointRounding.AwayFromZero);

    private static IReadOnlyList<SettlementLine> BuildLines(
        TaxForm form, decimal revenue, decimal costs, decimal dochod, decimal strata,
        decimal social, decimal health, decimal podstawa, decimal podstawaIp,
        decimal podatekIp, decimal podatekPozaIp, decimal podatek,
        decimal zaliczki, decimal doZaplaty, decimal nadplata, bool ipBox)
    {
        // Opisy odwołują się do części formularzy (stabilne między wariantami);
        // dokładne numery pozycji P_xx mapują buildery XML wg wariantu schemy.
        var lines = new List<SettlementLine>();

        switch (form)
        {
            case TaxForm.Ryczalt:
                lines.Add(new("PRZYCHOD", "Przychody z działalności gospodarczej (stawka 12%)",
                    Math.Round(revenue, 2), "PIT-28 cz. C — przychody objęte ryczałtem wg stawki 12% (usługi IT)"));
                lines.Add(new("SKLADKI_SPOLECZNE", "Składki na ubezpieczenia społeczne (odliczone)",
                    Math.Round(social, 2), "PIT-28 cz. D — składki zapłacone w roku podatkowym"));
                lines.Add(new("SKLADKA_ZDROWOTNA", "Składka zdrowotna — 50% zapłaconej (odliczona)",
                    Math.Round(health, 2), "PIT-28 cz. D — 50% składek na ubezpieczenie zdrowotne"));
                lines.Add(new("PODSTAWA", "Podstawa opodatkowania ryczałtem (po odliczeniach)",
                    podstawa, "PIT-28 cz. E — przychody po odliczeniach, w pełnych złotych"));
                lines.Add(new("PODATEK", "Ryczałt należny (12%)",
                    podatek, "PIT-28 cz. F — ryczałt od przychodów ewidencjonowanych"));
                break;

            default:
                var formCode = form == TaxForm.Liniowy ? "PIT-36L" : "PIT-36";
                lines.Add(new("PRZYCHOD", "Przychód z pozarolniczej działalności gospodarczej",
                    Math.Round(revenue, 2), $"{formCode} cz. E + załącznik PIT/B — przychód z JDG"));
                lines.Add(new("KOSZTY", "Koszty uzyskania przychodów",
                    Math.Round(costs, 2), $"{formCode} cz. E + załącznik PIT/B — koszty uzyskania"));
                lines.Add(new("DOCHOD", "Dochód (przychód − koszty)",
                    Math.Round(dochod, 2), $"{formCode} cz. E + załącznik PIT/B"));
                if (strata > 0m)
                    lines.Add(new("STRATA", "Strata z działalności gospodarczej",
                        Math.Round(strata, 2), $"{formCode} cz. E + załącznik PIT/B"));
                lines.Add(new("SKLADKI_SPOLECZNE", "Składki na ubezpieczenia społeczne (odliczone)",
                    Math.Round(social, 2), $"{formCode} — odliczenia od dochodu: składki zapłacone w roku"));
                if (form == TaxForm.Liniowy)
                    lines.Add(new("SKLADKA_ZDROWOTNA", "Składka zdrowotna (odliczona, do limitu)",
                        Math.Round(health, 2), "PIT-36L — odliczenie składki zdrowotnej do rocznego limitu"));
                lines.Add(new("PODSTAWA", "Podstawa obliczenia podatku",
                    podstawa, $"{formCode} — dochód po odliczeniach, w pełnych złotych"));
                if (ipBox)
                {
                    lines.Add(new("PODSTAWA_IP", "Podstawa opodatkowania stawką 5% (IP Box)",
                        podstawaIp, "Załącznik PIT/IP — kwalifikowany dochód z kwalifikowanych praw IP"));
                    lines.Add(new("PODATEK_IP", "Podatek od dochodu IP (5%)",
                        podatekIp, "Załącznik PIT/IP"));
                    lines.Add(new("PODATEK_POZA_IP", "Podatek od pozostałego dochodu",
                        podatekPozaIp, $"{formCode} — od podstawy pomniejszonej o dochód IP"));
                }
                lines.Add(new("PODATEK", "Podatek należny",
                    podatek, $"{formCode} — obliczony podatek po zaokrągleniu"));
                break;
        }

        lines.Add(new("ZALICZKI", "Zaliczki wpłacone w trakcie roku",
            Math.Round(zaliczki, 2), "Suma zaliczek na podatek / ryczałtu wpłaconych za rok podatkowy"));
        if (doZaplaty > 0m)
            lines.Add(new("DO_ZAPLATY", "Do zapłaty", doZaplaty,
                "Różnica między podatkiem należnym a sumą wpłaconych zaliczek"));
        else
            lines.Add(new("NADPLATA", "Nadpłata", nadplata,
                "Suma wpłaconych zaliczek przewyższa podatek należny"));

        return lines;
    }
}
