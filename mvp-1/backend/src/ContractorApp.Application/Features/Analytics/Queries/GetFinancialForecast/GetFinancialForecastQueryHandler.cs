using System.Globalization;
using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.Common.Tax;
using ContractorApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Analytics.Queries.GetFinancialForecast;

public class GetFinancialForecastQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetFinancialForecastQuery, FinancialForecastDto>
{
    private static readonly CultureInfo Pl = new("pl-PL");

    public async Task<FinancialForecastDto> Handle(GetFinancialForecastQuery request, CancellationToken ct)
    {
        var today = DateTime.Today;
        var year = request.Year ?? today.Year;
        var userId = currentUser.UserId;
        var taxForm = ParseTaxForm(request.TaxForm);
        var zusStage = ParseZusStage(request.ZusStage);
        var vatRate = request.VatRate ?? PolishTaxCalculator.VatStandardRate;

        // Liczba miesięcy traktowanych jako rzeczywiste (z zarejestrowanymi danymi):
        // rok przeszły → cały rok; rok bieżący → do bieżącego miesiąca; rok przyszły → 0.
        var actualThrough = year < today.Year ? 12 : year == today.Year ? today.Month : 0;

        var invoices = await db.Invoices
            .Include(i => i.Client)
            .Where(i => i.Client.UserId == userId
                     && i.IssueDate.Year == year
                     && i.Status != InvoiceStatus.Draft)
            .Select(i => new { i.IssueDate, i.TotalNet, i.TotalVat, i.Currency, i.ExchangeRate })
            .ToListAsync(ct);

        var expenses = await db.Expenses
            .Where(e => e.UserId == userId && e.Date.Year == year)
            .Select(e => new { e.Date, e.AmountPLN, e.IsVatDeductible })
            .ToListAsync(ct);

        static decimal ToPln(decimal amount, Currency currency, decimal? rate) =>
            currency == Currency.PLN ? amount : amount * (rate ?? 1m);

        // Agregaty rzeczywiste per miesiąc (indeks 1..12).
        var revByMonth = new decimal[13];
        var vatOutByMonth = new decimal[13];
        var costByMonth = new decimal[13];
        var vatInByMonth = new decimal[13];

        foreach (var i in invoices)
        {
            revByMonth[i.IssueDate.Month] += ToPln(i.TotalNet, i.Currency, i.ExchangeRate);
            vatOutByMonth[i.IssueDate.Month] += ToPln(i.TotalVat, i.Currency, i.ExchangeRate);
        }

        foreach (var e in expenses)
        {
            costByMonth[e.Date.Month] += e.AmountPLN;
            if (e.IsVatDeductible)
                vatInByMonth[e.Date.Month] += PolishTaxCalculator.InputVatFromGross(e.AmountPLN, vatRate);
        }

        // Run-rate: średnia z miesięcy rzeczywistych (mianownik ≥ 1).
        var denom = Math.Max(1, actualThrough);
        var ytdRevenue = Sum(revByMonth, 1, actualThrough);
        var ytdCosts = Sum(costByMonth, 1, actualThrough);
        var ytdVatOut = Sum(vatOutByMonth, 1, actualThrough);
        var ytdVatIn = Sum(vatInByMonth, 1, actualThrough);

        var avgRevenue = ytdRevenue / denom;
        var avgCosts = ytdCosts / denom;
        var avgVatOut = ytdVatOut / denom;
        var avgVatIn = ytdVatIn / denom;

        // Prognozowany przychód roczny — do wyboru progu składki zdrowotnej (ryczałt).
        var projectedAnnualRevenue = ytdRevenue + avgRevenue * (12 - actualThrough);
        var zusSocialMonthly = PolishTaxCalculator.MonthlyZusSocial(zusStage);

        var months = new List<MonthForecastDto>(12);

        // Stan kumulacyjny dla skali podatkowej (zaliczka = różnica narastająco).
        decimal cumIncome = 0m, cumZus = 0m, prevScaleTax = 0m;

        for (var m = 1; m <= 12; m++)
        {
            var isActual = m <= actualThrough;
            var revenue = isActual ? revByMonth[m] : avgRevenue;
            var costs = isActual ? costByMonth[m] : avgCosts;
            var income = revenue - costs;

            var vatOutput = isActual ? vatOutByMonth[m] : avgVatOut;
            var vatInput = isActual ? vatInByMonth[m] : avgVatIn;
            var vatPayable = vatOutput - vatInput;

            var zusHealth = PolishTaxCalculator.MonthlyHealth(taxForm, income, projectedAnnualRevenue);

            // Zaliczka PIT wg formy.
            cumIncome += income;
            cumZus += zusSocialMonthly;
            decimal incomeTax;
            switch (taxForm)
            {
                case TaxForm.Ryczalt:
                    incomeTax = PolishTaxCalculator.RyczaltMonthlyPit(revenue);
                    break;
                case TaxForm.Skala:
                    var cumBase = Math.Max(0m, cumIncome - cumZus - PolishTaxCalculator.TaxFreeAmount);
                    var scaleTax = PolishTaxCalculator.ScaleAnnualPit(cumBase);
                    incomeTax = Math.Max(0m, scaleTax - prevScaleTax);
                    prevScaleTax = Math.Max(prevScaleTax, scaleTax); // brak ujemnych zaliczek przy spadku dochodu
                    break;
                default: // liniowy
                    incomeTax = PolishTaxCalculator.LinearMonthlyPit(income, zusSocialMonthly);
                    break;
            }

            revenue = R(revenue);
            costs = R(costs);
            income = R(income);
            incomeTax = R(incomeTax);
            zusHealth = R(zusHealth);
            vatOutput = R(vatOutput);
            vatInput = R(vatInput);
            vatPayable = R(vatPayable);

            var totalObligations = incomeTax + zusSocialMonthly + zusHealth + Math.Max(0m, vatPayable);
            // VAT jest neutralny (pobrany od klienta ponad kwotę netto), więc nie obciąża cash-flow.
            var netCashFlow = revenue - costs - incomeTax - zusSocialMonthly - zusHealth;

            months.Add(new MonthForecastDto(
                m,
                new DateTime(year, m, 1).ToString("MMMM", Pl),
                isActual,
                revenue, costs, income,
                incomeTax, R(zusSocialMonthly), zusHealth,
                vatOutput, vatInput, vatPayable,
                R(totalObligations), R(netCashFlow)));
        }

        if (!(request.IncludeForecast ?? true))
            months.RemoveAll(m => !m.IsActual);

        var ytd = Aggregate(months.Where(x => x.IsActual));
        var fullYear = Aggregate(months);

        var assumptions = BuildAssumptions(taxForm, zusStage, denom, actualThrough, vatRate);

        // Scenariusz IP Box — liczony rocznie na zagregowanych sumach formy bazowej.
        // Dostępny tylko dla liniowego i skali; ZUS, składka zdrowotna i VAT bez zmian.
        IpBoxScenarioDto? ipBox = null;
        if ((request.IpBoxEnabled ?? false)
            && (taxForm == TaxForm.Liniowy || taxForm == TaxForm.Skala))
        {
            var pct = Math.Clamp(request.IpQualifyingPercent ?? 100m, 0m, 100m);
            var q = pct / 100m;
            var income = fullYear.Income;
            var social = fullYear.ZusSocial;

            var pitWithout = R(PolishTaxCalculator.BaseAnnualPit(taxForm, income, social));
            var pitWith = R(PolishTaxCalculator.IpBoxAnnualPit(taxForm, income, social, q));
            var savings = pitWithout - pitWith;

            var vat = Math.Max(0m, fullYear.VatPayable);
            var oblWithout = pitWithout + fullYear.ZusSocial + fullYear.ZusHealth + vat;
            var oblWith = pitWith + fullYear.ZusSocial + fullYear.ZusHealth + vat;
            var netWith = fullYear.Revenue - fullYear.Costs - pitWith - fullYear.ZusSocial - fullYear.ZusHealth;
            var rev = fullYear.Revenue;

            ipBox = new IpBoxScenarioDto(
                pct, q, pitWithout, pitWith, savings, R(savings / 12),
                R(oblWithout), R(oblWith), R(netWith),
                rev > 0 ? Math.Round(oblWithout / rev * 100, 1) : 0m,
                rev > 0 ? Math.Round(oblWith / rev * 100, 1) : 0m,
                BuildIpBoxVerdict(savings),
                IpBoxConditions());

            assumptions.Add(
                $"Scenariusz IP Box: 5% PIT na {pct:N0}% dochodu kwalifikowanego (Nexus {q:0.0#}); " +
                "ZUS, składka zdrowotna i VAT bez zmian. Wymaga odrębnej ewidencji i interpretacji KIS.");
        }

        return new FinancialForecastDto(
            year,
            TaxFormLabel(taxForm),
            ZusStageLabel(zusStage),
            actualThrough,
            R(avgRevenue),
            R(avgCosts),
            ytd,
            fullYear,
            months,
            assumptions,
            ipBox);
    }

    private static string BuildIpBoxVerdict(decimal savings) => savings switch
    {
        > 20_000m => $"Zdecydowanie TAK – oszczędzasz {savings:N0} PLN rocznie ({savings / 12:N0} PLN/mies.). Prowadź ewidencję IP Box.",
        > 5_000m => $"TAK – oszczędzasz {savings:N0} PLN rocznie. Sprawdź, czy koszty obsługi (doradca, KIS) nie zjadają korzyści.",
        > 0m => $"Nieznaczna korzyść ({savings:N0} PLN/rok) – rozważ, czy formalności IP Box są warte tego wysiłku.",
        _ => "NIE – IP Box nie generuje oszczędności przy tych danych."
    };

    private static List<string> IpBoxConditions() => new()
    {
        "Prawa IP muszą być wytworzone przez podatnika (programy komputerowe, algorytmy, API).",
        "Wymagana odrębna ewidencja projektów IP z podziałem czasu i kosztów.",
        "Współczynnik Nexus = A/(B+C+D); solo-developer bez zakupu IP → zazwyczaj 1,0.",
        "Dostępny wyłącznie przy podatku liniowym 19% lub skali (nie ryczałt).",
        "Zalecana interpretacja indywidualna KIS przed wdrożeniem (~3 mies.)."
    };

    private static decimal Sum(decimal[] arr, int from, int to)
    {
        decimal total = 0m;
        for (var i = from; i <= to && i < arr.Length; i++) total += arr[i];
        return total;
    }

    private static decimal R(decimal v) => Math.Round(v, 0, MidpointRounding.AwayFromZero);

    private static ForecastTotalsDto Aggregate(IEnumerable<MonthForecastDto> source)
    {
        var list = source.ToList();
        return new ForecastTotalsDto(
            list.Sum(x => x.Revenue),
            list.Sum(x => x.Costs),
            list.Sum(x => x.Income),
            list.Sum(x => x.IncomeTax),
            list.Sum(x => x.ZusSocial),
            list.Sum(x => x.ZusHealth),
            list.Sum(x => x.VatPayable),
            list.Sum(x => x.TotalObligations),
            list.Sum(x => x.NetCashFlow));
    }

    private static List<string> BuildAssumptions(
        TaxForm form, ZusStage stage, int denom, int actualThrough, decimal vatRate)
    {
        var healthNote = form switch
        {
            TaxForm.Ryczalt => "ryczałt — kwota stała wg progu przychodu rocznego",
            TaxForm.Skala => "skala — 9,0% dochodu (min. 314 PLN)",
            _ => "liniowy — 4,9% dochodu (min. 314 PLN)"
        };

        return new List<string>
        {
            actualThrough > 0
                ? $"Prognoza metodą run-rate: średnia z {denom} {MonthsWord(denom)} z danymi ekstrapolowana na pozostałe miesiące."
                : "Brak danych w wybranym roku — prognoza zerowa.",
            $"Forma opodatkowania: {TaxFormLabel(form)}. ZUS: {ZusStageLabel(stage)} ({PolishTaxCalculator.MonthlyZusSocial(stage):N2} PLN/mies.).",
            $"Składka zdrowotna: {healthNote}.",
            $"VAT należny z faktur (stawka krajowa). VAT naliczony szacowany jako {vatRate:P0} z kosztów oznaczonych jako podlegające odliczeniu.",
            "Wartości orientacyjne (stawki 2026). Skonsultuj z księgową przed decyzjami podatkowymi."
        };
    }

    private static string MonthsWord(int n) => n == 1 ? "miesiąca" : "miesięcy";

    private static TaxForm ParseTaxForm(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "ryczalt" or "ryczałt" => TaxForm.Ryczalt,
        "skala" => TaxForm.Skala,
        _ => TaxForm.Liniowy
    };

    private static ZusStage ParseZusStage(string? value) => value?.Trim().ToLowerInvariant().Replace("-", "_") switch
    {
        "ulga_na_start" => ZusStage.UlgaNaStart,
        "preferencyjny" => ZusStage.Preferencyjny,
        _ => ZusStage.Pelny
    };

    private static string TaxFormLabel(TaxForm form) => form switch
    {
        TaxForm.Ryczalt => "Ryczałt 12%",
        TaxForm.Skala => "Skala 12%/32%",
        _ => "Liniowy 19%"
    };

    private static string ZusStageLabel(ZusStage stage) => stage switch
    {
        ZusStage.UlgaNaStart => "Ulga na start",
        ZusStage.Preferencyjny => "Preferencyjny",
        _ => "Pełny (duży ZUS)"
    };
}
