using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Analytics.Commands.SaveForecastOverrides;

public class SaveForecastOverridesCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<SaveForecastOverridesCommand, Unit>
{
    public async Task<Unit> Handle(SaveForecastOverridesCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var year = request.Year;

        var existing = await db.ForecastOverrides
            .Where(o => o.UserId == userId && o.Year == year)
            .ToListAsync(ct);

        var byMonth = existing.ToDictionary(o => o.Month);

        switch (request.Action?.Trim().ToLowerInvariant())
        {
            case "reset":
                // Powrót do prognozy automatycznej — usuwamy wszystkie korekty roku.
                db.ForecastOverrides.RemoveRange(existing);
                break;

            case "clear":
                // Wyzerowanie — korekta 0/0 dla każdego miesiąca.
                for (var m = 1; m <= 12; m++)
                    Upsert(m, 0m, 0m);
                break;

            default: // "save"
                foreach (var item in request.Overrides ?? [])
                {
                    if (item.Month is < 1 or > 12) continue;
                    var revenue = item.Revenue is { } r ? Math.Max(0m, r) : (decimal?)null;
                    var cost = item.Cost is { } c ? Math.Max(0m, c) : (decimal?)null;

                    if (revenue is null && cost is null)
                    {
                        // Brak korekty — usuwamy ewentualny istniejący wpis.
                        if (byMonth.TryGetValue(item.Month, out var stale))
                            db.ForecastOverrides.Remove(stale);
                    }
                    else
                    {
                        Upsert(item.Month, revenue, cost);
                    }
                }
                break;
        }

        await db.SaveChangesAsync(ct);
        return Unit.Value;

        void Upsert(int month, decimal? revenue, decimal? cost)
        {
            if (byMonth.TryGetValue(month, out var row))
            {
                row.Revenue = revenue;
                row.Cost = cost;
                row.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                var created = new ForecastOverride
                {
                    UserId = userId,
                    Year = year,
                    Month = month,
                    Revenue = revenue,
                    Cost = cost,
                };
                db.ForecastOverrides.Add(created);
                byMonth[month] = created;
            }
        }
    }
}
