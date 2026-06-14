using System.Globalization;
using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.CurrencyExposure.Queries.GetCurrencyExposure;

public class GetCurrencyExposureQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetCurrencyExposureQuery, CurrencyExposureDto>
{
    private static readonly CultureInfo Pl = new("pl-PL");
    private static readonly int[] SensitivityScenarios = [-20, -10, 0, 10, 20];

    public async Task<CurrencyExposureDto> Handle(GetCurrencyExposureQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var year = request.Year;

        // ── 1. Faktury ─────────────────────────────────────────────────────────
        var invoices = await db.Invoices
            .Include(i => i.Client)
            .Where(i => i.Client.UserId == userId
                     && i.IssueDate.Year == year
                     && i.Status != InvoiceStatus.Draft
                     && i.Status != InvoiceStatus.Rejected)
            .Select(i => new
            {
                i.IssueDate,
                i.TotalNet,
                i.Currency,
                i.ExchangeRate,
            })
            .ToListAsync(ct);

        static decimal ToPln(decimal net, Currency cur, decimal? rate) =>
            cur == Currency.PLN ? net : net * (rate ?? 1m);

        // ── 2. Kursy NBP z tabeli ExchangeRate per miesiąc ────────────────────
        var fxRates = await db.ExchangeRates
            .Where(r => r.RateDate.Year == year
                     && (r.CurrencyCode == "EUR" || r.CurrencyCode == "USD"
                         || r.CurrencyCode == "GBP" || r.CurrencyCode == "CHF"))
            .Select(r => new { r.CurrencyCode, r.RateDate.Month, r.MidRate })
            .ToListAsync(ct);

        // Średnie kursy per miesiąc per waluta
        decimal? MonthlyAvgRate(string code, int month)
        {
            var subset = fxRates.Where(r => r.CurrencyCode == code && r.Month == month).ToList();
            return subset.Count > 0 ? subset.Average(r => r.MidRate) : null;
        }

        // ── 3. Podział walutowy ────────────────────────────────────────────────
        var breakdown = invoices
            .GroupBy(i => i.Currency)
            .Select(g =>
            {
                var currencyName = g.Key.ToString();
                var totalForeign = g.Sum(i => i.TotalNet);
                var totalPln = g.Sum(i => ToPln(i.TotalNet, g.Key, i.ExchangeRate));
                var rates = g.Where(i => i.ExchangeRate.HasValue).Select(i => i.ExchangeRate!.Value).ToList();
                return new CurrencyBreakdownDto(
                    currencyName,
                    Math.Round(totalForeign, 2),
                    Math.Round(totalPln, 0),
                    0m, // sharePercent — wyliczone poniżej
                    g.Count(),
                    rates.Count > 0 ? Math.Round(rates.Average(), 4) : 0m,
                    rates.Count > 0 ? Math.Round(rates.Min(), 4) : 0m,
                    rates.Count > 0 ? Math.Round(rates.Max(), 4) : 0m);
            })
            .OrderByDescending(b => b.TotalNetPln)
            .ToList();

        var totalRevenue = breakdown.Sum(b => b.TotalNetPln);
        breakdown = breakdown
            .Select(b => b with
            {
                SharePercent = totalRevenue > 0
                    ? Math.Round(b.TotalNetPln / totalRevenue * 100m, 1)
                    : 0m,
            })
            .ToList();

        var foreignRevenue = breakdown.Where(b => b.Currency != "PLN").Sum(b => b.TotalNetPln);
        var plnRevenue = totalRevenue - foreignRevenue;
        var foreignShare = totalRevenue > 0
            ? Math.Round(foreignRevenue / totalRevenue * 100m, 1) : 0m;

        // ── 4. Trend miesięczny ────────────────────────────────────────────────
        var monthlyTrend = new List<MonthlyFxDataDto>(12);
        for (var m = 1; m <= 12; m++)
        {
            var mi = invoices.Where(i => i.IssueDate.Month == m).ToList();

            decimal Rev(Currency cur) =>
                mi.Where(i => i.Currency == cur).Sum(i => ToPln(i.TotalNet, cur, i.ExchangeRate));

            var plnRev = Rev(Currency.PLN);
            var eurRev = Rev(Currency.EUR);
            var usdRev = Rev(Currency.USD);
            var gbpRev = Rev(Currency.GBP);
            var chfRev = Rev(Currency.CHF);

            monthlyTrend.Add(new MonthlyFxDataDto(
                m,
                new DateTime(year, m, 1).ToString("MMMM", Pl),
                Math.Round(plnRev, 0),
                Math.Round(eurRev, 0),
                Math.Round(usdRev, 0),
                Math.Round(gbpRev, 0),
                Math.Round(chfRev, 0),
                Math.Round(plnRev + eurRev + usdRev + gbpRev + chfRev, 0),
                MonthlyAvgRate("EUR", m),
                MonthlyAvgRate("USD", m),
                MonthlyAvgRate("GBP", m)));
        }

        // ── 5. Analiza wrażliwości ─────────────────────────────────────────────
        // Dla każdego scenariusza: ile zmieniłby się przychód PLN gdyby kursy były inne
        var eurRevTotal = invoices.Where(i => i.Currency == Currency.EUR).Sum(i => ToPln(i.TotalNet, Currency.EUR, i.ExchangeRate));
        var usdRevTotal = invoices.Where(i => i.Currency == Currency.USD).Sum(i => ToPln(i.TotalNet, Currency.USD, i.ExchangeRate));
        var gbpRevTotal = invoices.Where(i => i.Currency == Currency.GBP).Sum(i => ToPln(i.TotalNet, Currency.GBP, i.ExchangeRate));
        var chfRevTotal = invoices.Where(i => i.Currency == Currency.CHF).Sum(i => ToPln(i.TotalNet, Currency.CHF, i.ExchangeRate));

        var sensitivity = SensitivityScenarios.Select(pct =>
        {
            var factor = pct / 100m;
            var eurImpact = Math.Round(eurRevTotal * factor, 0);
            var usdImpact = Math.Round(usdRevTotal * factor, 0);
            var gbpImpact = Math.Round(gbpRevTotal * factor, 0);
            var chfImpact = Math.Round(chfRevTotal * factor, 0);
            var totalImpact = eurImpact + usdImpact + gbpImpact + chfImpact;
            return new SensitivityRowDto(
                pct,
                eurImpact,
                usdImpact,
                gbpImpact,
                chfImpact,
                totalImpact,
                Math.Round(totalRevenue + totalImpact, 0));
        }).ToList();

        return new CurrencyExposureDto(
            year,
            Math.Round(totalRevenue, 0),
            Math.Round(foreignRevenue, 0),
            Math.Round(plnRevenue, 0),
            foreignShare,
            breakdown,
            monthlyTrend,
            sensitivity);
    }
}
