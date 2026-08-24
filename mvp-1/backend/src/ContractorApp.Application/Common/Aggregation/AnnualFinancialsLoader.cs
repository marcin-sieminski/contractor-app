using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Common.Aggregation;

/// <summary>Roczne agregaty finansowe użytkownika; tablice indeksowane miesiącem 1..12.</summary>
public sealed record AnnualFinancials(
    decimal[] RevenueByMonth,
    decimal[] CostsByMonth,
    decimal AnnualRevenue,
    decimal AnnualCosts,
    IReadOnlyList<string> InvoicesMissingExchangeRate);

/// <summary>
/// Wspólne ładowanie rocznych przychodów (faktury) i kosztów (wydatki) — te same reguły
/// co prognoza finansowa: faktury bez statusu Draft po Client.UserId, przeliczenie walut
/// kursem z faktury, wydatki po AmountPLN (soft-delete filtrowany globalnie).
/// Faktury walutowe bez kursu są liczone 1:1 i raportowane do ostrzeżeń.
/// </summary>
public static class AnnualFinancialsLoader
{
    public static async Task<AnnualFinancials> LoadAsync(
        IApplicationDbContext db, string userId, int year, CancellationToken ct)
    {
        var invoices = await db.Invoices
            .Include(i => i.Client)
            .Where(i => i.Client.UserId == userId
                     && i.IssueDate.Year == year
                     && i.Status != InvoiceStatus.Draft)
            .Select(i => new { i.InvoiceNumber, i.IssueDate, i.TotalNet, i.Currency, i.ExchangeRate })
            .ToListAsync(ct);

        var expenses = await db.Expenses
            .Where(e => e.UserId == userId && e.Date.Year == year)
            .Select(e => new { e.Date, e.AmountPLN })
            .ToListAsync(ct);

        var revenue = new decimal[13];
        var costs = new decimal[13];
        var missingRate = new List<string>();

        foreach (var i in invoices)
        {
            if (i.Currency != Currency.PLN && i.ExchangeRate is null)
                missingRate.Add(i.InvoiceNumber);
            var pln = i.Currency == Currency.PLN ? i.TotalNet : i.TotalNet * (i.ExchangeRate ?? 1m);
            revenue[i.IssueDate.Month] += pln;
        }

        foreach (var e in expenses)
            costs[e.Date.Month] += e.AmountPLN;

        return new AnnualFinancials(
            revenue, costs,
            revenue.Sum(), costs.Sum(),
            missingRate);
    }
}
