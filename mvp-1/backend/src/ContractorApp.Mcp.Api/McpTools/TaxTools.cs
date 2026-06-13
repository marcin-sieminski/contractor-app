using System.ComponentModel;
using ContractorApp.Application.Common.Tax;
using ContractorApp.Application.Features.Invoices.Queries.GetFinancialSummary;
using MediatR;
using ModelContextProtocol.Server;

namespace ContractorApp.Mcp.Api.McpTools;

/// <summary>
/// MCP #2 — AI Asystent Podatkowy
/// Narzędzia do symulacji i prognozy podatków dla polskich kontraktoro B2B (2026).
/// </summary>
[McpServerToolType]
public class TaxTools(ISender mediator)
{
    // ── Stałe podatkowe 2026 ────────────────────────────────────────────────────

    // ZUS społeczne / miesiąc
    private static readonly decimal ZusSocialFull = 1_803.33m; // duży ZUS
    private static readonly decimal ZusSocialPref = 478.00m;   // preferencyjny (obniżony)
    private static readonly decimal ZusSocialUlga = 0m;        // ulga na start

    // ZUS zdrowotne minimalne
    private static readonly decimal HealthMin = 314.00m;

    // Składka zdrowotna ryczałt (progi rocznego przychodu)
    private static readonly decimal HealthRyczaltLow  = 419.00m;   // ≤ 60 000 PLN/rok
    private static readonly decimal HealthRyczaltMid  = 699.00m;   // 60 001 – 300 000 PLN/rok
    private static readonly decimal HealthRyczaltHigh = 1_257.00m; // > 300 000 PLN/rok

    // Stawki PIT
    private static readonly decimal RyczaltIT    = 0.12m;  // 12% ryczałt usługi IT
    private static readonly decimal LinearPIT    = 0.19m;  // 19% liniowy
    private static readonly decimal IpBoxPIT     = 0.05m;  // 5% IP Box
    private static readonly decimal HealthLinear = 0.049m; // 4,9% zdrowotna liniowy
    private static readonly decimal HealthScale  = 0.09m;  // 9,0% zdrowotna skala

    // ── Narzędzie 1: Porównanie form podatkowych ────────────────────────────────

    [McpServerTool(Name = "compare_tax_forms")]
    [Description(
        "Porównuje roczne obciążenia podatkowe dla ryczałtu 12%, liniowego 19% i skali podatkowej " +
        "(12%/32%) przy podanym miesięcznym przychodzie. Zwraca tabelę z PIT, składką zdrowotną, " +
        "ZUS społecznym i łącznym kosztem dla każdej formy oraz wskazuje optymalną.")]
    public object CompareTaxForms(
        [Description("Miesięczny przychód brutto w PLN, np. 25000")] decimal monthlyRevenuePln,
        [Description("Miesięczne koszty uzyskania przychodu w PLN (odliczane dla liniowego/skali). Domyślnie 0.")] decimal? monthlyExpensesPln = null,
        [Description("Etap ZUS: ulga_na_start | preferencyjny | pelny. Domyślnie: pelny.")] string? zusStage = null,
        [Description("Rok podatkowy – tylko informacyjnie; obliczenia wg stawek 2026. Domyślnie bieżący rok.")] int? year = null)
    {
        var annualRevenue  = monthlyRevenuePln * 12;
        var annualExpenses = (monthlyExpensesPln ?? 0) * 12;
        var annualIncome   = Math.Max(0, annualRevenue - annualExpenses);
        var socialMonth    = ParseZusStage(zusStage);
        var annualSocial   = socialMonth * 12;

        // Ryczałt 12% – podatek od przychodu brutto
        var taxRyczalt    = Math.Round(annualRevenue * RyczaltIT, 0);
        var healthRyczalt = RyczaltHealthMonth(annualRevenue) * 12;
        var totalRyczalt  = taxRyczalt + healthRyczalt + annualSocial;

        // Liniowy 19% – dochód = przychód − koszty − ZUS społeczny
        var baseLinear   = Math.Max(0, annualIncome - annualSocial);
        var taxLinear    = Math.Round(baseLinear * LinearPIT, 0);
        var healthLinear = Math.Max(HealthMin, annualIncome / 12 * HealthLinear) * 12;
        var totalLinear  = taxLinear + healthLinear + annualSocial;

        // Skala 12%/32% – formuła z kwotą zmniejszającą (wspólna implementacja)
        var taxSkala = PolishTaxCalculator.ScaleAnnualPit(Math.Max(0, annualIncome - annualSocial));
        var healthSkala = Math.Max(HealthMin, annualIncome / 12 * HealthScale) * 12;
        var totalSkala  = taxSkala + healthSkala + annualSocial;

        static string Rate(decimal lacznie, decimal revenue) =>
            $"{Math.Round(revenue > 0 ? lacznie / revenue * 100 : 0, 1)}%";

        var rows = new[]
        {
            new { Forma = "Ryczałt 12%",   PIT_roczny = taxRyczalt, Skladka_zdrowotna = Math.Round(healthRyczalt, 0), ZUS_spoleczne = Math.Round(annualSocial, 0), Lacznie_obciazenie = Math.Round(totalRyczalt, 0), Netto_roczne = Math.Round(annualRevenue - totalRyczalt, 0), Netto_miesieczne = Math.Round((annualRevenue - totalRyczalt) / 12, 0), Efektywna_stawka = Rate(totalRyczalt, annualRevenue) },
            new { Forma = "Liniowy 19%",   PIT_roczny = taxLinear,  Skladka_zdrowotna = Math.Round(healthLinear,  0), ZUS_spoleczne = Math.Round(annualSocial, 0), Lacznie_obciazenie = Math.Round(totalLinear,  0), Netto_roczne = Math.Round(annualIncome  - totalLinear,  0), Netto_miesieczne = Math.Round((annualIncome  - totalLinear)  / 12, 0), Efektywna_stawka = Rate(totalLinear, annualRevenue) },
            new { Forma = "Skala 12%/32%", PIT_roczny = taxSkala,   Skladka_zdrowotna = Math.Round(healthSkala,   0), ZUS_spoleczne = Math.Round(annualSocial, 0), Lacznie_obciazenie = Math.Round(totalSkala,   0), Netto_roczne = Math.Round(annualIncome  - totalSkala,   0), Netto_miesieczne = Math.Round((annualIncome  - totalSkala)   / 12, 0), Efektywna_stawka = Rate(totalSkala, annualRevenue) },
        };

        var totals  = new[] { totalRyczalt, totalLinear, totalSkala };
        var formy   = new[] { "Ryczałt 12%", "Liniowy 19%", "Skala 12%/32%" };
        var bestIdx = Array.IndexOf(totals, totals.Min());

        return new
        {
            Rok                = year ?? DateTime.Today.Year,
            Miesieczny_przychod = monthlyRevenuePln,
            Miesieczne_koszty  = monthlyExpensesPln ?? 0,
            ZUS_etap           = zusStage ?? "pelny",
            ZUS_spoleczne_mies = socialMonth,
            Tabela             = rows,
            Optymalna_forma    = formy[bestIdx],
            Nota = "Ryczałt od przychodu brutto; liniowy i skala od dochodu (przychód−koszty−ZUS społeczny). " +
                   "Składka zdrowotna: ryczałt progowa, liniowy 4,9% dochodu, skala 9,0% dochodu. " +
                   "Stawki 2026. Przed wyborem formy skonsultuj z doradcą podatkowym."
        };
    }

    // ── Narzędzie 2: Kalkulator ZUS ─────────────────────────────────────────────

    [McpServerTool(Name = "calculate_zus")]
    [Description(
        "Oblicza miesięczne składki ZUS dla danego etapu działalności (ulga na start, preferencyjny, " +
        "pełny ZUS) i podaje ile miesięcy pozostało do zmiany progu. " +
        "Opcjonalnie przyjmuje datę rejestracji DG, by wyliczyć termin zmiany progu.")]
    public object CalculateZus(
        [Description("Etap ZUS: ulga_na_start | preferencyjny | pelny. Wymagany.")] string stage,
        [Description("Data rejestracji działalności w formacie YYYY-MM-DD (np. '2024-01-15'). " +
                     "Opcjonalna – potrzebna do obliczenia pozostałych miesięcy do zmiany progu.")] string? businessStartDate = null,
        [Description("Miesięczny dochód w PLN – używany do szacunku składki zdrowotnej. Domyślnie 8000 PLN.")] decimal? monthlyIncomePln = null)
    {
        var startDate = businessStartDate is not null
            ? DateOnly.Parse(businessStartDate)
            : (DateOnly?)null;

        var today = DateOnly.FromDateTime(DateTime.Today);
        int? monthsElapsed = startDate.HasValue
            ? (today.Year - startDate.Value.Year) * 12 + (today.Month - startDate.Value.Month)
            : null;

        var stageKey = stage.ToLowerInvariant().Replace("-", "_");
        string stageName, nextStage, opis;
        decimal social;
        int? monthsToNext;

        switch (stageKey)
        {
            case "ulga_na_start":
                stageName    = "Ulga na start (0–6 mies.)";
                social       = ZusSocialUlga;
                nextStage    = "Preferencyjny ZUS (obniżone składki)";
                monthsToNext = monthsElapsed.HasValue ? Math.Max(0, 6 - monthsElapsed.Value) : null;
                opis         = "Zwolnienie z ZUS społecznego przez pierwsze 6 miesięcy. Płacisz tylko składkę zdrowotną.";
                break;
            case "preferencyjny":
                stageName    = "Preferencyjny ZUS (miesiące 7–30)";
                social       = ZusSocialPref;
                nextStage    = "Pełny ZUS (duży ZUS)";
                monthsToNext = monthsElapsed.HasValue ? Math.Max(0, 30 - monthsElapsed.Value) : null;
                opis         = "Obniżone składki przez 24 miesiące po uldze (łącznie 30 mies. od rejestracji DG).";
                break;
            default:
                stageName    = "Pełny ZUS (duży ZUS)";
                social       = ZusSocialFull;
                nextStage    = "Brak – to najwyższy próg ZUS";
                monthsToNext = null;
                opis         = "Pełne składki ZUS. Jeśli roczny przychód < 120 000 PLN, możesz wnioskować o Mały ZUS+.";
                break;
        }

        var incomeForHealth  = monthlyIncomePln ?? 8_000m;
        var annualRevenue    = incomeForHealth * 12;
        var health           = RyczaltHealthMonth(annualRevenue); // przybliżenie dla ryczałtu
        var totalMonthly     = social + health;

        string? malyzusPodpowiedz = stageKey == "pelny" && annualRevenue < 120_000m
            ? "Twój przychód poniżej 120 000 PLN/rok – możesz kwalifikować się do Mały ZUS+ (niższe składki). " +
              "Zgłoś do ZUS do 20 stycznia lub w ciągu 7 dni od zmiany progu."
            : null;

        var nextChangeDate = (startDate.HasValue && monthsToNext.HasValue && monthsToNext > 0)
            ? startDate.Value.AddMonths(stageKey == "ulga_na_start" ? 6 : 30).ToString("yyyy-MM-dd")
            : null;

        return new
        {
            Etap                     = stageName,
            ZUS_spoleczne_mies       = social,
            Skladka_zdrowotna_mies   = health,
            Lacznie_miesiac          = totalMonthly,
            Lacznie_rok              = totalMonthly * 12,
            Nastepny_etap            = nextStage,
            Miesiecy_do_zmiany       = monthsToNext,
            Data_zmiany_progu        = nextChangeDate,
            Opis                     = opis,
            Maly_ZUS_plus            = malyzusPodpowiedz
        };
    }

    // ── Narzędzie 3: Prognoza roczna (na danych z aplikacji) ────────────────────

    [McpServerTool(Name = "forecast_annual_tax")]
    [Description(
        "Prognozuje roczne zobowiązania podatkowe na podstawie rzeczywistych faktur i kosztów " +
        "zapisanych w aplikacji (ekstrapolacja bieżącego tempa na 12 miesięcy). " +
        "Pokazuje: prognozowany przychód/dochód, PIT, ZUS, składkę zdrowotną, netto i zaliczkę miesięczną.")]
    public async Task<object> ForecastAnnualTax(
        [Description("Forma podatkowa: ryczalt | liniowy | skala. Domyślnie: liniowy.")] string? taxForm,
        [Description("Etap ZUS: ulga_na_start | preferencyjny | pelny. Domyślnie: pelny.")] string? zusStage,
        CancellationToken ct)
    {
        var today   = DateTime.Today;
        var year    = today.Year;
        var month   = today.Month;
        var summary = await mediator.Send(new GetFinancialSummaryQuery(year), ct);

        // Ekstrapolacja na cały rok
        var annualRevenue  = month > 0 ? summary.TotalNetPLN / month * 12 : 0;
        var annualExpenses = month > 0 ? summary.TotalExpensesPLN / month * 12 : 0;
        var annualIncome   = Math.Max(0, annualRevenue - annualExpenses);
        var socialMonth    = ParseZusStage(zusStage);
        var annualSocial   = socialMonth * 12;

        var form = (taxForm ?? "liniowy").ToLowerInvariant();
        decimal tax, health;

        switch (form)
        {
            case "ryczalt":
                tax    = Math.Round(annualRevenue * RyczaltIT, 0);
                health = RyczaltHealthMonth(annualRevenue) * 12;
                break;
            case "skala":
                tax    = PolishTaxCalculator.ScaleAnnualPit(Math.Max(0, annualIncome - annualSocial));
                health = Math.Max(HealthMin, annualIncome / 12 * HealthScale) * 12;
                break;
            default: // liniowy
                tax    = Math.Round(Math.Max(0, annualIncome - annualSocial) * LinearPIT, 0);
                health = Math.Max(HealthMin, annualIncome / 12 * HealthLinear) * 12;
                break;
        }

        var totalBurden = tax + health + annualSocial;
        var nettoBase   = form == "ryczalt" ? annualRevenue : annualIncome;
        var nettoRoczne = nettoBase - totalBurden;

        return new
        {
            Rok                   = year,
            Miesiac_obliczen      = month,
            Forma_podatkowa       = form,
            Dane_YTD = new
            {
                Przychod_YTD    = Math.Round(summary.TotalNetPLN, 0),
                Koszty_YTD      = Math.Round(summary.TotalExpensesPLN, 0),
                Miesiecy_danych = month
            },
            Prognoza_roczna = new
            {
                Przychod        = Math.Round(annualRevenue, 0),
                Koszty          = Math.Round(annualExpenses, 0),
                Dochod          = Math.Round(annualIncome, 0),
                PIT             = tax,
                ZUS_spoleczne   = Math.Round(annualSocial, 0),
                Zdrowotna       = Math.Round(health, 0),
                Lacznie         = Math.Round(totalBurden, 0),
                Efektywna_stawka = $"{Math.Round(annualRevenue > 0 ? totalBurden / annualRevenue * 100 : 0, 1)}%",
                Netto_roczne    = Math.Round(nettoRoczne, 0),
                Netto_miesieczne = Math.Round(nettoRoczne / 12, 0)
            },
            Zaliczki_miesieczne = new
            {
                PIT_zaliczka     = Math.Round(tax / 12, 0),
                ZUS_spoleczne    = socialMonth,
                Zdrowotna        = Math.Round(health / 12, 0),
                Lacznie          = Math.Round(totalBurden / 12, 0)
            },
            Pozostale_miesiace  = 12 - month,
            Nota                = $"Ekstrapolacja na {12 - month} pozostałych miesięcy roku. " +
                                  "Uwzględnij sezonowość – rzeczywiste kwoty mogą się różnić."
        };
    }

    // ── Narzędzie 4: Kalkulator IP Box ──────────────────────────────────────────

    [McpServerTool(Name = "calculate_ip_box")]
    [Description(
        "Odpowiada na pytanie 'Czy IP Box mi się opłaca?' – oblicza roczne oszczędności " +
        "podatkowe dzięki preferencyjnej stawce 5% PIT na dochód z praw własności intelektualnej " +
        "(programy komputerowe, algorytmy). Dostępny wyłącznie przy formie liniowej 19%. " +
        "Przykład: 'Zarabiam 25 000 PLN/mies., czy IP Box mi się opłaca?' → konkretna kwota oszczędności.")]
    public object CalculateIpBox(
        [Description("Miesięczny przychód brutto z usług IT / praw IP w PLN, np. 25000")] decimal monthlyRevenuePln,
        [Description("Miesięczne koszty uzyskania przychodu w PLN. Domyślnie 0.")] decimal? monthlyExpensesPln = null,
        [Description("Procent przychodu kwalifikowanego jako IP Box (0–100). Domyślnie 100. " +
                     "Dla solo-developera bez zakupu licencji/podwykonawców współczynnik Nexus = 1,0.")] decimal? ipPercent = null,
        [Description("Etap ZUS: ulga_na_start | preferencyjny | pelny. Domyślnie: pelny.")] string? zusStage = null)
    {
        var annualRevenue  = monthlyRevenuePln * 12;
        var annualExpenses = (monthlyExpensesPln ?? 0) * 12;
        var annualIncome   = Math.Max(0, annualRevenue - annualExpenses);
        var nexus          = Math.Clamp(ipPercent ?? 100m, 0, 100) / 100m;
        var ipIncome       = annualIncome * nexus;
        var nonIpIncome    = annualIncome * (1 - nexus);
        var socialMonth    = ParseZusStage(zusStage);
        var annualSocial   = socialMonth * 12;

        // Bez IP Box – cały dochód opodatkowany liniowo 19%
        var baseWithout    = Math.Max(0, annualIncome - annualSocial);
        var taxWithout     = Math.Round(baseWithout * LinearPIT, 0);
        var health         = Math.Max(HealthMin, annualIncome / 12 * HealthLinear) * 12;

        // Z IP Box – 5% na dochód IP, 19% na pozostały dochód
        var ipBase         = Math.Max(0, ipIncome - annualSocial * nexus);
        var nonIpBase      = Math.Max(0, nonIpIncome - annualSocial * (1 - nexus));
        var taxWith        = Math.Round(ipBase * IpBoxPIT + nonIpBase * LinearPIT, 0);

        var savings        = taxWithout - taxWith;
        var totalBurdenWithout = taxWithout + health + annualSocial;
        var totalBurdenWith    = taxWith    + health + annualSocial;

        var ocena = savings > 20_000m
            ? $"Zdecydowanie TAK – oszczędzasz {savings:N0} PLN rocznie ({savings / 12:N0} PLN/mies.). Prowadź ewidencję IP Box."
            : savings > 5_000m
            ? $"TAK – oszczędzasz {savings:N0} PLN rocznie. Sprawdź, czy koszty obsługi (doradca, KIS) nie zjadają korzyści."
            : savings > 0m
            ? $"Nieznaczna korzyść ({savings:N0} PLN/rok) – rozważ, czy formalności IP Box są warte tego wysiłku."
            : "NIE – IP Box nie generuje oszczędności przy tych danych.";

        return new
        {
            Dane_wejsciowe = new
            {
                Miesieczny_przychod = monthlyRevenuePln,
                Roczny_przychod     = annualRevenue,
                Roczny_dochod       = Math.Round(annualIncome, 0),
                Nexus_IP            = nexus == 1m ? "1,0 (100% – typowo dla solo-developera)" : $"{nexus:P0}",
                ZUS_etap            = zusStage ?? "pelny"
            },
            Porownanie = new
            {
                Podatek_liniowy_bez_IP_Box = taxWithout,
                Podatek_z_IP_Box           = taxWith,
                Roczne_oszczednosci        = savings,
                Miesieczne_oszczednosci    = Math.Round(savings / 12, 0),
                Lacznie_bez_IP_Box         = Math.Round(totalBurdenWithout, 0),
                Lacznie_z_IP_Box           = Math.Round(totalBurdenWith, 0),
                Efektywna_stawka_bez       = $"{Math.Round(totalBurdenWithout / annualRevenue * 100, 1)}%",
                Efektywna_stawka_z         = $"{Math.Round(totalBurdenWith    / annualRevenue * 100, 1)}%"
            },
            Odpowiedz = savings > 0 ? "TAK – IP Box się opłaca" : "NIE – brak korzyści",
            Ocena     = ocena,
            Warunki_IP_Box = new[]
            {
                "Prawa IP muszą być wytworzone przez podatnika (programy komputerowe, algorytmy, API)",
                "Wymagana odrębna ewidencja projektów IP z podziałem czasu i kosztów",
                "Współczynnik Nexus = A/(B+C+D); solo-developer bez zakupu IP → zazwyczaj 1,0",
                "Dostępny wyłącznie przy podatku liniowym 19% (nie ryczałt, nie skala)",
                "Zalecana interpretacja indywidualna KIS przed wdrożeniem (~3 mies.)"
            }
        };
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static decimal ParseZusStage(string? stage) => stage?.ToLowerInvariant() switch
    {
        "ulga_na_start" => ZusSocialUlga,
        "preferencyjny" => ZusSocialPref,
        _ => ZusSocialFull
    };

    private static decimal RyczaltHealthMonth(decimal annualRevenue) => annualRevenue switch
    {
        <= 60_000m  => HealthRyczaltLow,
        <= 300_000m => HealthRyczaltMid,
        _           => HealthRyczaltHigh
    };
}
