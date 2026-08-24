using System.Globalization;
using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.Common.Tax;
using ContractorApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Invoices.Queries.GetFinancialSummary;

public class GetFinancialSummaryQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetFinancialSummaryQuery, FinancialSummaryDto>
{
    public async Task<FinancialSummaryDto> Handle(GetFinancialSummaryQuery request, CancellationToken ct)
    {
        var year = request.Year ?? DateTime.Today.Year;
        var userId = currentUser.UserId;

        var invoices = await db.Invoices
            .Include(i => i.Client)
            .Where(i => i.Client.UserId == userId
                     && i.IssueDate.Year == year
                     && i.Status != InvoiceStatus.Draft)
            .ToListAsync(ct);

        var expenses = await db.Expenses
            .Where(e => e.UserId == userId && e.Date.Year == year)
            .ToListAsync(ct);

        static decimal ToPln(decimal amount, Currency currency, decimal? rate) =>
            currency == Currency.PLN ? amount : amount * (rate ?? 1m);

        var totalNetPLN = invoices.Sum(i => ToPln(i.TotalNet, i.Currency, i.ExchangeRate));
        var totalGrossPLN = invoices.Sum(i => ToPln(i.TotalGross, i.Currency, i.ExchangeRate));
        var totalExpensesPLN = expenses.Sum(e => e.AmountPLN);

        var taxBase = Math.Max(0m, totalNetPLN - totalExpensesPLN);
        var estimatedTax = Math.Round(taxBase * PolishTaxCalculator.LinearPitRate, 2);

        var culture = new CultureInfo("pl-PL");
        var byMonth = Enumerable.Range(1, 12)
            .Select(m => new MonthlySummaryDto(
                m,
                new DateTime(year, m, 1).ToString("MMMM", culture),
                invoices.Where(i => i.IssueDate.Month == m)
                        .Sum(i => ToPln(i.TotalNet, i.Currency, i.ExchangeRate)),
                expenses.Where(e => e.Date.Month == m).Sum(e => e.AmountPLN)))
            .ToList();

        return new FinancialSummaryDto(
            year, totalNetPLN, totalGrossPLN,
            totalExpensesPLN, estimatedTax, PolishTaxCalculator.ZusSocialFull, byMonth);
    }
}
