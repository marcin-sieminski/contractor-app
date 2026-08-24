using System.Globalization;
using ContractorApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.WorkAnalytics.Queries.GetWorkAnalytics;

public class GetWorkAnalyticsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetWorkAnalyticsQuery, WorkAnalyticsDto>
{
    private static readonly CultureInfo Pl = new("pl-PL");

    public async Task<WorkAnalyticsDto> Handle(GetWorkAnalyticsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var year = request.Year;
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Ukończone wpisy czasowe dla danego roku
        var entries = await db.TimeEntries
            .Include(te => te.Project)
            .ThenInclude(p => p.Client)
            .Where(te => te.Project.Client.UserId == userId
                      && te.StartedAt.Year == year
                      && te.StoppedAt != null
                      && te.DeletedAt == null)
            .Select(te => new
            {
                te.Id,
                te.StartedAt,
                te.StoppedAt,
                te.DurationMinutes,
                te.IsInvoiced,
                ProjectName = te.Project.Name,
                ClientName = te.Project.Client.Name,
            })
            .ToListAsync(ct);

        // Niezafakturowane wpisy starsze niż 30 dni (globalnie — nie tylko za dany rok)
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        var alertEntries = await db.TimeEntries
            .Include(te => te.Project)
            .ThenInclude(p => p.Client)
            .Where(te => te.Project.Client.UserId == userId
                      && !te.IsInvoiced
                      && te.StoppedAt != null
                      && te.StoppedAt < cutoff
                      && te.DeletedAt == null)
            .Select(te => new
            {
                te.Id,
                te.StartedAt,
                te.DurationMinutes,
                ProjectName = te.Project.Name,
                ClientName = te.Project.Client.Name,
            })
            .OrderBy(te => te.StartedAt)
            .ToListAsync(ct);

        static double DurH(int? durationMinutes) =>
            (durationMinutes ?? 0) / 60.0;

        // Heatmap: godziny per dzień
        var dayGroups = entries
            .GroupBy(e => DateOnly.FromDateTime(e.StartedAt.LocalDateTime))
            .Select(g => new DayWorkDto(
                g.Key.ToString("yyyy-MM-dd"),
                Math.Round(g.Sum(e => DurH(e.DurationMinutes)), 2),
                g.Any(e => e.IsInvoiced)))
            .OrderBy(d => d.Date)
            .ToList();

        // Miesięczne podsumowanie
        var months = new List<MonthWorkDto>(12);
        for (var m = 1; m <= 12; m++)
        {
            var me = entries.Where(e => e.StartedAt.Month == m).ToList();
            var total = me.Sum(e => DurH(e.DurationMinutes));
            var invoiced = me.Where(e => e.IsInvoiced).Sum(e => DurH(e.DurationMinutes));
            var pending = total - invoiced;
            var pct = total > 0 ? Math.Round((decimal)(invoiced / total * 100), 1) : 0m;
            var worked = me.Select(e => DateOnly.FromDateTime(e.StartedAt.LocalDateTime))
                           .Distinct()
                           .Count(d => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday);
            months.Add(new MonthWorkDto(
                m,
                new DateTime(year, m, 1).ToString("MMMM", Pl),
                Math.Round(total, 1),
                Math.Round(invoiced, 1),
                Math.Round(pending, 1),
                pct,
                worked));
        }

        // Podsumowanie roczne
        var totalH = entries.Sum(e => DurH(e.DurationMinutes));
        var invH = entries.Where(e => e.IsInvoiced).Sum(e => DurH(e.DurationMinutes));
        var pendH = totalH - invH;
        var invPct = totalH > 0 ? Math.Round((decimal)(invH / totalH * 100), 1) : 0m;

        var workedDays = entries
            .Select(e => DateOnly.FromDateTime(e.StartedAt.LocalDateTime))
            .Distinct()
            .Count(d => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday);

        // Dni robocze w roku (pon-pt) — do today jeśli bieżący rok
        var periodEnd = year < today.Year ? new DateOnly(year, 12, 31) : today;
        var businessDays = 0;
        for (var d = new DateOnly(year, 1, 1); d <= periodEnd; d = d.AddDays(1))
        {
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                businessDays++;
        }

        var freeDays = Math.Max(0, businessDays - workedDays);
        var avgPerDay = workedDays > 0 ? Math.Round(totalH / workedDays, 1) : 0.0;
        var weeksInPeriod = (periodEnd.DayNumber - new DateOnly(year, 1, 1).DayNumber + 1) / 7.0;
        var avgPerWeek = weeksInPeriod > 0 ? Math.Round(totalH / weeksInPeriod, 1) : 0.0;

        var summary = new WorkAnalyticsSummaryDto(
            Math.Round(totalH, 1),
            Math.Round(invH, 1),
            Math.Round(pendH, 1),
            invPct,
            workedDays,
            businessDays,
            freeDays,
            avgPerDay,
            avgPerWeek,
            alertEntries.Count);

        var alerts = alertEntries.Select(e => new UnbilledAlertDto(
            e.Id,
            e.ProjectName,
            e.ClientName,
            Math.Round(DurH(e.DurationMinutes), 1),
            e.StartedAt.LocalDateTime.ToString("yyyy-MM-dd"),
            (int)(DateTime.Today - e.StartedAt.LocalDateTime.Date).TotalDays))
            .ToList();

        return new WorkAnalyticsDto(year, summary, months, dayGroups, alerts);
    }
}
