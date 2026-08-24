using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Profitability.Queries.GetClientProfitability;

public class GetClientProfitabilityQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetClientProfitabilityQuery, ProfitabilityDto>
{
    public async Task<ProfitabilityDto> Handle(GetClientProfitabilityQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var year = request.Year;
        var month = request.Month;

        // Faktury (nie-draft) per klient / projekt
        var invoiceQuery = db.Invoices
            .Include(i => i.Client)
            .Where(i => i.Client.UserId == userId
                     && i.IssueDate.Year == year
                     && i.Status != InvoiceStatus.Draft);

        if (month.HasValue)
            invoiceQuery = invoiceQuery.Where(i => i.IssueDate.Month == month.Value);

        var invoiceRows = await invoiceQuery
            .Select(i => new
            {
                i.ClientId,
                ClientName = i.Client.Name,
                ClientNip = i.Client.Nip,
                NetPln = i.Currency == Currency.PLN
                    ? i.TotalNet
                    : i.TotalNet * (i.ExchangeRate ?? 1m),
            })
            .ToListAsync(ct);

        // Time entries per projekt — tylko z DurationMinutes lub StoppedAt (zakończone)
        var teQuery = db.TimeEntries
            .Include(te => te.Project)
            .ThenInclude(p => p.Client)
            .Where(te => te.Project.Client.UserId == userId
                      && te.StartedAt.Year == year
                      && te.StoppedAt != null);

        if (month.HasValue)
            teQuery = teQuery.Where(te => te.StartedAt.Month == month.Value);

        var timeRows = await teQuery
            .Select(te => new
            {
                te.ProjectId,
                ProjectName = te.Project.Name,
                ClientId = te.Project.ClientId,
                // DurationMinutes jest ustawiany przy StopTimer; fallback na różnicę dat
                Minutes = te.DurationMinutes.HasValue
                    ? te.DurationMinutes.Value
                    : (int)(te.StoppedAt!.Value - te.StartedAt).TotalMinutes
                      + te.AccumulatedSeconds / 60,
            })
            .ToListAsync(ct);

        // Grupuj faktury per klient
        var invoiceByClient = invoiceRows
            .GroupBy(i => i.ClientId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Grupuj time entries per klient → projekt
        var timeByClientProject = timeRows
            .GroupBy(te => te.ClientId)
            .ToDictionary(g => g.Key, g =>
                g.GroupBy(te => te.ProjectId).ToDictionary(pg => pg.Key, pg => pg.ToList()));

        // Zbierz wszystkich klientów, którzy mają faktury lub time entries
        var allClientIds = invoiceByClient.Keys
            .Union(timeByClientProject.Keys)
            .ToList();

        // Wczytaj nazwy/NIP klientów dla tych, którzy mają tylko time entries (bez faktur)
        var clientMeta = await db.Clients
            .Where(c => c.UserId == userId && allClientIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name, c.Nip })
            .ToDictionaryAsync(c => c.Id, ct);

        var clients = new List<ClientProfitabilityDto>();

        foreach (var clientId in allClientIds)
        {
            var invoices = invoiceByClient.GetValueOrDefault(clientId) ?? [];
            var projectGroups = timeByClientProject.GetValueOrDefault(clientId) ?? [];

            var clientRevenue = invoices.Sum(i => i.NetPln);
            var clientName = invoices.FirstOrDefault()?.ClientName
                ?? clientMeta.GetValueOrDefault(clientId)?.Name ?? "—";
            var clientNip = invoices.FirstOrDefault()?.ClientNip
                ?? clientMeta.GetValueOrDefault(clientId)?.Nip ?? "";
            var invoiceCount = invoices.Count;

            // Per-projekt: hours z time entries; revenue z faktur (przypisz do projektu proporcjonalnie?)
            // Uproszczenie: faktury nie mają bezpośredniego pola ProjectId — przypisz cały przychód klienta.
            // Dla poszczególnych projektów pokazujemy tylko godziny + stawkę na bazie stawki klienta.
            var projects = projectGroups.Select(pg =>
            {
                var hours = pg.Value.Sum(te => te.Minutes) / 60.0;
                // Efektywna stawka projektu = udział godzinowy projektu × przychód klienta / godziny projektu
                // Jeśli brak faktur → stawka z konfiguracji projektu (niedostępna tu; pokazuj 0)
                var rate = hours > 0 && clientRevenue > 0
                    ? clientRevenue / (decimal)hours
                    : 0m;
                return new ProjectProfitabilityDto(
                    pg.Key,
                    pg.Value.First().ProjectName,
                    0m, // per-projekt przychód bez join z line items — brak bezpośredniego pola
                    hours,
                    rate);
            }).OrderByDescending(p => p.BillableHours).ToList();

            var totalClientHours = projects.Sum(p => p.BillableHours);
            var effectiveRate = totalClientHours > 0
                ? clientRevenue / (decimal)totalClientHours
                : 0m;

            clients.Add(new ClientProfitabilityDto(
                clientId,
                clientName,
                clientNip,
                clientRevenue,
                totalClientHours,
                Math.Round(effectiveRate, 2),
                0m, // revenueShare — policzone po zliczeniu wszystkich
                invoiceCount,
                projects));
        }

        var totalRevenue = clients.Sum(c => c.RevenuePln);
        var totalHours = clients.Sum(c => c.BillableHours);
        var overallRate = totalHours > 0 ? totalRevenue / (decimal)totalHours : 0m;

        // Dołącz RevenueShare
        clients = clients
            .Select(c => c with
            {
                RevenueSharePercent = totalRevenue > 0
                    ? Math.Round(c.RevenuePln / totalRevenue * 100m, 1)
                    : 0m,
            })
            .OrderByDescending(c => c.RevenuePln)
            .ToList();

        // Ryzyko koncentracji: jeden klient > 50% przychodu
        var topClient = clients.FirstOrDefault();
        var hasRisk = topClient?.RevenueSharePercent > 50m;
        var warning = hasRisk
            ? $"Klient \"{topClient!.ClientName}\" generuje {topClient.RevenueSharePercent:F0}% Twojego przychodu — wysoka koncentracja."
            : null;

        var periodLabel = month.HasValue
            ? new System.Globalization.CultureInfo("pl-PL").DateTimeFormat.GetMonthName(month.Value) + $" {year}"
            : $"Cały rok {year}";

        return new ProfitabilityDto(
            year,
            periodLabel,
            totalRevenue,
            Math.Round(totalHours, 1),
            Math.Round(overallRate, 2),
            hasRisk,
            warning,
            clients);
    }
}
