using System.Globalization;
using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.IpBox.Queries.GetIpBoxProgress;

public class GetIpBoxProgressQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetIpBoxProgressQuery, IpBoxProgressDto>
{
    private static readonly CultureInfo Pl = new("pl-PL");
    private const decimal NexusCoefficient = 1.0m;

    public async Task<IpBoxProgressDto> Handle(GetIpBoxProgressQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var year = request.Year;

        var entries = await db.TimeEntries
            .Include(te => te.Project)
            .ThenInclude(p => p.Client)
            .Where(te => te.Project.Client.UserId == userId
                      && te.StartedAt.Year == year
                      && te.StoppedAt != null)
            .Select(te => new
            {
                te.Id,
                te.StartedAt,
                te.DurationMinutes,
                te.IsIpWork,
                te.IpWorkDescription,
                te.Description,
                ProjectName = te.Project.Name,
                IsIpProject = te.Project.IsIpProject
            })
            .ToListAsync(ct);

        var invoices = await db.Invoices
            .Include(i => i.Client)
            .Where(i => i.Client.UserId == userId
                     && i.IssueDate.Year == year
                     && i.Status != InvoiceStatus.Draft
                     && i.Status != InvoiceStatus.Rejected)
            .Select(i => new
            {
                i.IssueDate.Month,
                i.TotalNet,
                i.Currency,
                i.ExchangeRate
            })
            .ToListAsync(ct);

        static decimal ToPln(decimal amount, Currency currency, decimal? rate) =>
            currency == Currency.PLN ? amount : amount * (rate ?? 1m);

        var revenueByMonth = invoices
            .GroupBy(i => i.Month)
            .ToDictionary(g => g.Key, g => g.Sum(i => ToPln(i.TotalNet, i.Currency, i.ExchangeRate)));

        var totalRevenuePln = revenueByMonth.Values.Sum();

        var totalHours = entries.Sum(e => (e.DurationMinutes ?? 0) / 60m);
        var ipHours = entries.Where(e => e.IsIpWork || e.IsIpProject).Sum(e => (e.DurationMinutes ?? 0) / 60m);
        var ipPct = totalHours > 0 ? Math.Round(ipHours / totalHours * 100, 1) : 0m;

        var monthlyData = Enumerable.Range(1, 12).Select(m =>
        {
            var mEntries = entries.Where(e => e.StartedAt.Month == m).ToList();
            var mTotal = mEntries.Sum(e => (e.DurationMinutes ?? 0) / 60m);
            var mIp = mEntries.Where(e => e.IsIpWork || e.IsIpProject).Sum(e => (e.DurationMinutes ?? 0) / 60m);
            var mRatio = mTotal > 0 ? mIp / mTotal : 0m;
            var mRevenue = revenueByMonth.GetValueOrDefault(m, 0m);
            var mIpRevenue = Math.Round(mRevenue * mRatio, 2);
            var mQualifying = Math.Round(mIpRevenue * NexusCoefficient, 2);
            return new MonthIpDataDto(
                m,
                new DateTime(year, m, 1).ToString("MMMM", Pl),
                Math.Round(mIp, 2),
                Math.Round(mTotal, 2),
                mIpRevenue,
                Math.Round(mRevenue, 2),
                mQualifying);
        }).ToList();

        var ratio = totalHours > 0 ? ipHours / totalHours : 0m;
        var ipRevenuePln = Math.Round(totalRevenuePln * ratio, 2);
        var qualifyingPln = Math.Round(ipRevenuePln * NexusCoefficient, 2);
        // Savings = difference between liniowy 19% and IP Box 5%
        var savingsPln = Math.Round(qualifyingPln * 0.14m, 2);

        var today = DateTime.Today;
        var monthsCompleted = year < today.Year ? 12 : today.Month;
        var projectedSavings = monthsCompleted > 0
            ? Math.Round(savingsPln / monthsCompleted * 12, 2)
            : 0m;

        var unfilled = entries
            .Where(e => (e.IsIpWork || e.IsIpProject) && string.IsNullOrWhiteSpace(e.IpWorkDescription))
            .Select(e => new UnfilledIpEntryDto(
                e.Id,
                e.ProjectName,
                e.StartedAt.LocalDateTime.ToString("yyyy-MM-dd"),
                e.DurationMinutes,
                e.Description))
            .OrderByDescending(e => e.StartedAt)
            .ToList();

        return new IpBoxProgressDto(
            year,
            NexusCoefficient,
            Math.Round(totalHours, 1),
            Math.Round(ipHours, 1),
            ipPct,
            Math.Round(totalRevenuePln, 2),
            ipRevenuePln,
            qualifyingPln,
            savingsPln,
            projectedSavings,
            monthlyData,
            unfilled);
    }
}
