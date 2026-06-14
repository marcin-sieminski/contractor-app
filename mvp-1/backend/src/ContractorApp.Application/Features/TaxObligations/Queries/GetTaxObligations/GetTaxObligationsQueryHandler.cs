using System.Globalization;
using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.Common.Tax;
using ContractorApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.TaxObligations.Queries.GetTaxObligations;

public class GetTaxObligationsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetTaxObligationsQuery, TaxObligationsDto>
{
    private static readonly CultureInfo Pl = new("pl-PL");

    public async Task<TaxObligationsDto> Handle(GetTaxObligationsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var year = request.Year;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var taxForm = ParseTaxForm(request.TaxForm);
        var zusStage = ParseZusStage(request.ZusStage);

        // Wczytaj surowe dane z DB — bez konwersji walutowej w SQL
        var invoiceRows = await db.Invoices
            .Include(i => i.Client)
            .Where(i => i.Client.UserId == userId
                     && i.IssueDate.Year == year
                     && i.Status != InvoiceStatus.Draft)
            .Select(i => new { i.IssueDate, i.TotalNet, i.TotalVat, i.Currency, i.ExchangeRate })
            .ToListAsync(ct);

        var expenseRows = await db.Expenses
            .Where(e => e.UserId == userId && e.Date.Year == year)
            .Select(e => new { e.Date, e.AmountPLN })
            .ToListAsync(ct);

        var payments = await db.TaxPayments
            .Where(p => p.UserId == userId && p.Year == year)
            .OrderBy(p => p.Month).ThenBy(p => p.Type)
            .ToListAsync(ct);

        // Konwertuj do PLN w pamięci (tak jak w GetFinancialForecastQueryHandler)
        static decimal ToPln(decimal amount, Currency currency, decimal? rate) =>
            currency == Currency.PLN ? amount : amount * (rate ?? 1m);

        var revByMonth = new decimal[13];
        var vatOutByMonth = new decimal[13];
        foreach (var inv in invoiceRows)
        {
            revByMonth[inv.IssueDate.Month] += ToPln(inv.TotalNet, inv.Currency, inv.ExchangeRate);
            vatOutByMonth[inv.IssueDate.Month] += ToPln(inv.TotalVat, inv.Currency, inv.ExchangeRate);
        }

        var costByMonth = new decimal[13];
        foreach (var exp in expenseRows)
            costByMonth[exp.Date.Month] += exp.AmountPLN;

        // Szacowany przychód roczny — do wyboru progu składki zdrowotnej (ryczałt)
        var projectedAnnualRevenue = revByMonth.Skip(1).Sum();
        var zusSocialMonthly = PolishTaxCalculator.MonthlyZusSocial(zusStage);

        // Kumulacja PIT skali (narastająco)
        decimal cumIncome = 0m, cumZus = 0m, prevScaleTax = 0m;

        var months = new List<MonthObligationDto>(12);
        for (var m = 1; m <= 12; m++)
        {
            var revenue = revByMonth[m];
            var costs = costByMonth[m];
            var income = revenue - costs;
            var vatOut = vatOutByMonth[m];
            var vatIn = PolishTaxCalculator.InputVatFromGross(costs, PolishTaxCalculator.VatStandardRate);
            var vatPayable = Math.Max(0m, vatOut - vatIn);

            var zusHealth = PolishTaxCalculator.MonthlyHealth(taxForm, income, projectedAnnualRevenue);

            cumIncome += income;
            cumZus += zusSocialMonthly;
            decimal pit;
            switch (taxForm)
            {
                case TaxForm.Ryczalt:
                    pit = PolishTaxCalculator.RyczaltMonthlyPit(revenue);
                    break;
                case TaxForm.Skala:
                    var cumBase = Math.Max(0m, cumIncome - cumZus);
                    var scaleTax = PolishTaxCalculator.ScaleAnnualPit(cumBase);
                    pit = Math.Max(0m, scaleTax - prevScaleTax);
                    prevScaleTax = Math.Max(prevScaleTax, scaleTax);
                    break;
                default: // liniowy
                    pit = PolishTaxCalculator.LinearMonthlyPit(income, zusSocialMonthly);
                    break;
            }

            pit = R(pit);
            zusHealth = R(zusHealth);
            vatPayable = R(vatPayable);
            var zusSocial = R(zusSocialMonthly);
            var totalDue = pit + zusSocial + zusHealth + vatPayable;

            // Terminy płatności za miesiąc m: ZUS i PIT → 20-ty następnego miesiąca; VAT → 25-ty
            var zusDue = NextMonthDay(year, m, 20);
            var pitDue = NextMonthDay(year, m, 20);
            var vatDue = NextMonthDay(year, m, 25);

            var monthPayments = payments.Where(p => p.Month == m).ToList();
            var pitPaid = R(monthPayments.Where(p => p.Type == TaxPaymentType.PIT).Sum(p => p.Amount));
            var zusSocialPaid = R(monthPayments.Where(p => p.Type == TaxPaymentType.ZusSocial).Sum(p => p.Amount));
            var zusHealthPaid = R(monthPayments.Where(p => p.Type == TaxPaymentType.ZusHealth).Sum(p => p.Amount));
            var vatPaid = R(monthPayments.Where(p => p.Type == TaxPaymentType.VAT).Sum(p => p.Amount));
            var totalPaid = pitPaid + zusSocialPaid + zusHealthPaid + vatPaid;

            var isActual = year < today.Year || (year == today.Year && m <= today.Month);
            var status = DetermineStatus(today, vatDue, isActual, totalDue, totalPaid);

            months.Add(new MonthObligationDto(
                m,
                new DateTime(year, m, 1).ToString("MMMM", Pl),
                isActual,
                pit, zusSocial, zusHealth, vatPayable, R(totalDue),
                zusDue.ToString("yyyy-MM-dd"),
                pitDue.ToString("yyyy-MM-dd"),
                vatDue.ToString("yyyy-MM-dd"),
                monthPayments.Select(p => new TaxPaymentRecordDto(
                    p.Id, p.Year, p.Month,
                    p.Type.ToString(),
                    p.Amount,
                    p.PaidAt?.ToString("yyyy-MM-dd"),
                    p.Notes)).ToList(),
                pitPaid, zusSocialPaid, zusHealthPaid, vatPaid, R(totalPaid),
                status));
        }

        var summary = new TaxObligationsSummaryDto(
            months.Sum(m => m.PitDue),
            months.Sum(m => m.ZusSocialDue),
            months.Sum(m => m.ZusHealthDue),
            months.Sum(m => m.VatDue),
            months.Sum(m => m.TotalDue),
            months.Sum(m => m.PitPaid),
            months.Sum(m => m.ZusSocialPaid),
            months.Sum(m => m.ZusHealthPaid),
            months.Sum(m => m.VatPaid),
            months.Sum(m => m.TotalPaid),
            months.Sum(m => m.TotalDue) - months.Sum(m => m.TotalPaid));

        return new TaxObligationsDto(year, TaxFormLabel(taxForm), ZusStageLabel(zusStage), summary, months);
    }

    private static string DetermineStatus(
        DateOnly today, DateOnly latestDueDate, bool isActual, decimal totalDue, decimal totalPaid)
    {
        if (!isActual) return "future";
        if (totalDue <= 0m || totalPaid >= totalDue) return "paid";
        if (totalPaid > 0m) return latestDueDate < today ? "overdue" : "partial";
        return latestDueDate < today ? "overdue" : "due";
    }

    // Dzień termin płatności: day-ty miesiąca następującego po m w danym roku
    private static DateOnly NextMonthDay(int year, int month, int day)
    {
        var next = new DateOnly(year, month, 1).AddMonths(1);
        return new DateOnly(next.Year, next.Month, Math.Min(day, DateTime.DaysInMonth(next.Year, next.Month)));
    }

    private static decimal R(decimal v) => Math.Round(v, 0, MidpointRounding.AwayFromZero);

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
