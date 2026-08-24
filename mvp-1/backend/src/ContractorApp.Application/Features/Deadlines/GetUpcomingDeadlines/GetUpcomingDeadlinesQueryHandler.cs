using MediatR;

namespace ContractorApp.Application.Features.Deadlines.GetUpcomingDeadlines;

public class GetUpcomingDeadlinesQueryHandler : IRequestHandler<GetUpcomingDeadlinesQuery, List<DeadlineDto>>
{
    public Task<List<DeadlineDto>> Handle(GetUpcomingDeadlinesQuery request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutoff = today.AddDays(request.DaysAhead);
        var deadlines = new List<DeadlineDto>();

        // Generate deadlines for current month and next 2 months of obligations
        for (var offset = 0; offset <= 2; offset++)
        {
            var refDate = today.AddMonths(offset);

            // ZUS social: due 20th of the following month
            AddDeadline(deadlines, today, cutoff,
                "ZUS — składki społeczne",
                DayOf(refDate.Year, refDate.Month, 20).AddMonths(1),
                $"Termin płatności składek ZUS społecznych za {MonthName(refDate)}.");

            // VAT-7 + JPK_V7M: due 25th of the following month
            AddDeadline(deadlines, today, cutoff,
                "VAT-7 + JPK_V7M",
                DayOf(refDate.Year, refDate.Month, 25).AddMonths(1),
                $"Deklaracja VAT-7 i plik JPK_V7M za {MonthName(refDate)}.");

            // PIT-5L quarterly advance: 20th after end of quarter (Q1→Apr20, Q2→Jul20, Q3→Oct20, Q4→Jan20)
            if (refDate.Month is 3 or 6 or 9 or 12)
            {
                var quarterEnd = new DateOnly(refDate.Year, refDate.Month, 1);
                AddDeadline(deadlines, today, cutoff,
                    "PIT-5L — zaliczka kwartalna",
                    quarterEnd.AddMonths(1).AddDays(19), // 20th of next month
                    $"Zaliczka na podatek dochodowy PIT-5L za Q{(refDate.Month / 3)} {refDate.Year}.");
            }
        }

        var result = deadlines
            .DistinctBy(d => (d.Name, d.Date))
            .OrderBy(d => d.Date)
            .ToList();

        return Task.FromResult(result);
    }

    private static void AddDeadline(
        List<DeadlineDto> list,
        DateOnly today,
        DateOnly cutoff,
        string name,
        DateOnly date,
        string description)
    {
        if (date > cutoff) return;
        var daysUntil = date.DayNumber - today.DayNumber;
        list.Add(new DeadlineDto(name, date, description, daysUntil, daysUntil < 0));
    }

    private static DateOnly DayOf(int year, int month, int day) =>
        new(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));

    private static string MonthName(DateOnly date) =>
        date.ToString("MMMM yyyy", new System.Globalization.CultureInfo("pl-PL"));
}
