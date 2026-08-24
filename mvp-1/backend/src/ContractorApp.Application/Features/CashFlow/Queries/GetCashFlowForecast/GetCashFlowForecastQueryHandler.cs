using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.Common.Tax;
using ContractorApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.CashFlow.Queries.GetCashFlowForecast;

public class GetCashFlowForecastQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetCashFlowForecastQuery, CashFlowForecastDto>
{
    public async Task<CashFlowForecastDto> Handle(GetCashFlowForecastQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var windowEnd = today.AddDays(90);
        var taxForm = ParseTaxForm(request.TaxForm);
        var zusStage = ParseZusStage(request.ZusStage);

        static decimal ToPln(decimal amount, Currency currency, decimal? rate) =>
            currency == Currency.PLN ? amount : amount * (rate ?? 1m);

        // ── 1. Faktury ─────────────────────────────────────────────────────────
        // Ostatnie 3 miesiące → średni miesięczny przychód do projekcji podatków
        var threeMonthsAgo = new DateOnly(today.Year, today.Month, 1).AddMonths(-3);
        var recentInvoices = await db.Invoices
            .Include(i => i.Client)
            .Where(i => i.Client.UserId == userId
                     && i.Status != InvoiceStatus.Draft
                     && i.Status != InvoiceStatus.Rejected
                     && i.IssueDate >= threeMonthsAgo)
            .Select(i => new
            {
                i.IssueDate,
                i.DueDate,
                i.TotalNet,
                i.TotalVat,
                i.TotalGross,
                i.Currency,
                i.ExchangeRate,
                i.InvoiceNumber,
                ClientName = i.Client.Name,
            })
            .ToListAsync(ct);

        // Faktury wymagalne w oknie 90 dni lub przeterminowane (jeszcze nierozliczone)
        var upcomingInvoices = await db.Invoices
            .Include(i => i.Client)
            .Where(i => i.Client.UserId == userId
                     && i.Status != InvoiceStatus.Draft
                     && i.Status != InvoiceStatus.Rejected
                     && i.DueDate >= today.AddDays(-30)  // max 30 dni wstecz (przeterminowane)
                     && i.DueDate <= windowEnd)
            .Select(i => new
            {
                i.DueDate,
                i.TotalGross,
                i.Currency,
                i.ExchangeRate,
                i.InvoiceNumber,
                ClientName = i.Client.Name,
            })
            .ToListAsync(ct);

        // Średni przychód netto i VAT z ostatnich 3 miesięcy
        var recentMonthsCount = Math.Max(1,
            recentInvoices.Select(i => (i.IssueDate.Year, i.IssueDate.Month)).Distinct().Count());
        var avgMonthlyRevenue = recentInvoices.Sum(i => ToPln(i.TotalNet, i.Currency, i.ExchangeRate))
                                / recentMonthsCount;
        var avgMonthlyVatOut = recentInvoices.Sum(i => ToPln(i.TotalVat, i.Currency, i.ExchangeRate))
                               / recentMonthsCount;

        // Średnie koszty z ostatnich 3 miesięcy
        var recentExpenses = await db.Expenses
            .Where(e => e.UserId == userId && e.Date >= threeMonthsAgo)
            .Select(e => new { e.Date, e.AmountPLN })
            .ToListAsync(ct);
        var avgMonthlyCosts = recentExpenses.Any()
            ? recentExpenses.Sum(e => e.AmountPLN) /
              Math.Max(1, recentExpenses.Select(e => (e.Date.Year, e.Date.Month)).Distinct().Count())
            : 0m;

        var avgMonthlyIncome = avgMonthlyRevenue - avgMonthlyCosts;
        var zusSocialMonthly = PolishTaxCalculator.MonthlyZusSocial(zusStage);

        // DSO: średnia liczba dni między datą wystawienia a terminem płatności
        var avgDso = recentInvoices.Count > 0
            ? recentInvoices.Average(i => (i.DueDate.ToDateTime(TimeOnly.MinValue) -
                                           i.IssueDate.ToDateTime(TimeOnly.MinValue)).TotalDays)
            : 30.0;

        // ── 2. Zobowiązania podatkowe w oknie 90 dni ───────────────────────────
        // Miesiące, których termin płatności wypada w oknie (od miesiąca previous do +3)
        var fromFiscalMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        var toFiscalMonth = fromFiscalMonth.AddMonths(4);

        // Wczytaj już zarejestrowane wpłaty
        var paidPayments = await db.TaxPayments
            .Where(tp => tp.UserId == userId
                      && tp.Year >= fromFiscalMonth.Year
                      && tp.Year <= toFiscalMonth.Year)
            .ToListAsync(ct);

        var taxEvents = new List<CashFlowEventDto>();
        var totalObligations = 0m;
        var totalAlreadyPaid = 0m;

        for (var fm = fromFiscalMonth; fm <= toFiscalMonth; fm = fm.AddMonths(1))
        {
            var y = fm.Year;
            var m = fm.Month;

            // Terminy: ZUS/PIT → 20-ty następnego miesiąca; VAT → 25-ty
            var zusPitDue = NextMonthDay(y, m, 20);
            var vatDue = NextMonthDay(y, m, 25);

            if (zusPitDue < today || zusPitDue > windowEnd)
                continue; // poza oknem

            // Oblicz zobowiązania
            var pit = R(taxForm == TaxForm.Ryczalt
                ? PolishTaxCalculator.RyczaltMonthlyPit(avgMonthlyRevenue)
                : PolishTaxCalculator.LinearMonthlyPit(avgMonthlyIncome, zusSocialMonthly));
            var zusHealth = R(PolishTaxCalculator.MonthlyHealth(taxForm, avgMonthlyIncome, avgMonthlyRevenue * 12));
            var zusS = R(zusSocialMonthly);
            var vat = R(Math.Max(0m,
                avgMonthlyVatOut - PolishTaxCalculator.InputVatFromGross(avgMonthlyCosts, PolishTaxCalculator.VatStandardRate)));

            // Sprawdź co już zapłacone
            decimal PaidFor(TaxPaymentType type) =>
                paidPayments.Where(p => p.Year == y && p.Month == m && p.Type == type).Sum(p => p.Amount);

            var pitPaid = PaidFor(TaxPaymentType.PIT);
            var zusSPaid = PaidFor(TaxPaymentType.ZusSocial);
            var zusHPaid = PaidFor(TaxPaymentType.ZusHealth);
            var vatPaid = PaidFor(TaxPaymentType.VAT);

            var monthLabel = $"{y}-{m:D2}";

            AddObligation(taxEvents, zusPitDue, "tax", $"ZUS społeczny {monthLabel}",
                zusS, zusSPaid, ref totalObligations, ref totalAlreadyPaid);
            AddObligation(taxEvents, zusPitDue, "tax", $"ZUS zdrowotny {monthLabel}",
                zusHealth, zusHPaid, ref totalObligations, ref totalAlreadyPaid);
            AddObligation(taxEvents, zusPitDue, "tax", $"Zaliczka PIT {monthLabel}",
                pit, pitPaid, ref totalObligations, ref totalAlreadyPaid);

            if (vatDue >= today && vatDue <= windowEnd)
                AddObligation(taxEvents, vatDue, "tax", $"VAT {monthLabel}",
                    vat, vatPaid, ref totalObligations, ref totalAlreadyPaid);
        }

        // ── 3. Buduj dzienny plan przepływów ───────────────────────────────────
        // Słownik: data → (inflows, outflows)
        var dayFlows = new Dictionary<DateOnly, (decimal In, decimal Out, List<string> Labels)>();

        // Inflows z faktur
        decimal totalExpectedInflows = 0m;
        foreach (var inv in upcomingInvoices)
        {
            var gross = ToPln(inv.TotalGross, inv.Currency, inv.ExchangeRate);
            totalExpectedInflows += gross;
            if (!dayFlows.ContainsKey(inv.DueDate))
                dayFlows[inv.DueDate] = (0m, 0m, []);
            var (In, Out, Labels) = dayFlows[inv.DueDate];
            dayFlows[inv.DueDate] = (In + gross, Out, Labels);
            Labels.Add($"Faktura {inv.InvoiceNumber} ({inv.ClientName})");
        }

        // Outflows z podatków
        foreach (var ev in taxEvents.Where(e => e.Status != "paid"))
        {
            var d = DateOnly.Parse(ev.Date);
            if (!dayFlows.ContainsKey(d))
                dayFlows[d] = (0m, 0m, []);
            var (In, Out, Labels) = dayFlows[d];
            dayFlows[d] = (In, Out + ev.Amount, Labels);
            Labels.Add(ev.Label);
        }

        // Generuj dzienną tablicę
        var balance = request.StartingBalance;
        var days = new List<CashFlowDayDto>(91);
        var allEvents = new List<CashFlowEventDto>(taxEvents);

        for (var d = today; d <= windowEnd; d = d.AddDays(1))
        {
            dayFlows.TryGetValue(d, out var flow);
            var (inflows, outflows, _) = flow;

            // Dodaj inflow events
            foreach (var inv in upcomingInvoices.Where(i => i.DueDate == d))
            {
                var gross = ToPln(inv.TotalGross, inv.Currency, inv.ExchangeRate);
                allEvents.Add(new CashFlowEventDto(
                    d.ToString("yyyy-MM-dd"),
                    "invoice",
                    $"Faktura {inv.InvoiceNumber} — {inv.ClientName}",
                    gross,
                    d < today ? "overdue" : "ok"));
            }

            var balanceBefore = balance + inflows;
            string? obligationStatus = null;
            if (outflows > 0)
            {
                obligationStatus = balanceBefore >= outflows * 1.5m ? "ok"
                    : balanceBefore >= outflows ? "tight"
                    : "danger";
            }

            balance = balanceBefore - outflows;

            days.Add(new CashFlowDayDto(
                d.ToString("yyyy-MM-dd"),
                DayName(d.DayOfWeek),
                R(inflows),
                R(outflows),
                R(balance),
                outflows > 0,
                obligationStatus));
        }

        // Zaktualizuj status tax events na podstawie salda
        var balanceSim = request.StartingBalance;
        var dayBalanceMap = days.ToDictionary(d => d.Date, d => d.Balance);

        var finalEvents = allEvents
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Kind == "invoice" ? 0 : 1)
            .Select(e =>
            {
                if (e.Kind != "tax" || e.Status == "paid") return e;
                dayBalanceMap.TryGetValue(e.Date, out var bal);
                var newStatus = bal > e.Amount * 0.5m ? "ok"
                    : bal > 0 ? "tight"
                    : "danger";
                return e with { Status = newStatus };
            })
            .ToList();

        var avgMonthlyObligation = totalObligations / Math.Max(1m,
            taxEvents.Select(e => e.Date[..7]).Distinct().Count());

        return new CashFlowForecastDto(
            today.ToString("yyyy-MM-dd"),
            windowEnd.ToString("yyyy-MM-dd"),
            request.StartingBalance,
            R(totalExpectedInflows),
            R(totalObligations),
            R(totalAlreadyPaid),
            R(request.StartingBalance + totalExpectedInflows - totalObligations),
            R(avgMonthlyObligation),
            R(avgMonthlyObligation),
            Math.Round(avgDso, 1),
            days,
            finalEvents);
    }

    private static void AddObligation(
        List<CashFlowEventDto> events,
        DateOnly dueDate,
        string kind,
        string label,
        decimal amount,
        decimal paid,
        ref decimal totalObligation,
        ref decimal totalPaid)
    {
        var remaining = Math.Max(0m, amount - paid);
        totalObligation += remaining;
        totalPaid += Math.Min(paid, amount);

        if (remaining <= 0m)
        {
            events.Add(new CashFlowEventDto(dueDate.ToString("yyyy-MM-dd"), kind, label, amount, "paid"));
            return;
        }

        events.Add(new CashFlowEventDto(dueDate.ToString("yyyy-MM-dd"), kind, label, remaining, "ok"));
    }

    private static DateOnly NextMonthDay(int year, int month, int day)
    {
        var next = new DateOnly(year, month, 1).AddMonths(1);
        return new DateOnly(next.Year, next.Month, Math.Min(day, DateTime.DaysInMonth(next.Year, next.Month)));
    }

    private static decimal R(decimal v) => Math.Round(v, 0, MidpointRounding.AwayFromZero);

    private static string DayName(DayOfWeek dow) => dow switch
    {
        DayOfWeek.Monday => "Pn",
        DayOfWeek.Tuesday => "Wt",
        DayOfWeek.Wednesday => "Śr",
        DayOfWeek.Thursday => "Cz",
        DayOfWeek.Friday => "Pt",
        DayOfWeek.Saturday => "So",
        _ => "Nd"
    };

    private static TaxForm ParseTaxForm(string? v) => v?.Trim().ToLowerInvariant() switch
    {
        "ryczalt" or "ryczałt" => TaxForm.Ryczalt,
        "skala" => TaxForm.Skala,
        _ => TaxForm.Liniowy
    };

    private static ZusStage ParseZusStage(string? v) => v?.Trim().ToLowerInvariant().Replace("-", "_") switch
    {
        "ulga_na_start" => ZusStage.UlgaNaStart,
        "preferencyjny" => ZusStage.Preferencyjny,
        _ => ZusStage.Pelny
    };
}
