using System.ComponentModel;
using ContractorApp.Application.DTOs;
using ContractorApp.Application.Features.Clients.Queries.GetClients;
using ContractorApp.Application.Features.TimeEntries.Commands.CreateManualEntry;
using ContractorApp.Application.Features.TimeEntries.Commands.DeleteTimeEntry;
using ContractorApp.Application.Features.TimeEntries.Commands.PauseTimer;
using ContractorApp.Application.Features.TimeEntries.Commands.ResumeTimer;
using ContractorApp.Application.Features.TimeEntries.Commands.StartTimer;
using ContractorApp.Application.Features.TimeEntries.Commands.StopTimer;
using ContractorApp.Application.Features.TimeEntries.Commands.UpdateTimeEntry;
using ContractorApp.Application.Features.TimeEntries.Queries.GetActiveTimer;
using ContractorApp.Application.Features.TimeEntries.Queries.GetTimeEntries;
using MediatR;
using ModelContextProtocol.Server;

namespace ContractorApp.Mcp.Api.McpTools;

[McpServerToolType]
public class TimeTools(ISender mediator)
{
    [McpServerTool(Name = "get_time_entries")]
    [Description("Returns time entries for the authenticated user. Optionally filter by date range (ISO 8601, e.g. '2025-05-01') and client name (case-insensitive substring).")]
    public async Task<object> GetTimeEntries(
        [Description("Start date, e.g. '2025-05-01'")] string? from,
        [Description("End date, e.g. '2025-05-31'")] string? to,
        [Description("Client name filter (case-insensitive substring)")] string? clientName,
        CancellationToken ct)
    {
        var fromDate = from is not null ? DateOnly.Parse(from) : (DateOnly?)null;
        var toDate = to is not null ? DateOnly.Parse(to) : (DateOnly?)null;

        var entries = await mediator.Send(new GetTimeEntriesQuery(fromDate, toDate, null, true), ct);

        if (clientName is not null)
            entries = entries
                .Where(e => e.ClientName.Contains(clientName, StringComparison.OrdinalIgnoreCase))
                .ToList();

        return entries;
    }

    [McpServerTool(Name = "get_time_summary")]
    [Description("Returns a summary of hours worked and income per client/project for a given month. Format: 'YYYY-MM'. Defaults to current month.")]
    public async Task<object> GetTimeSummary(
        [Description("Month in YYYY-MM format, e.g. '2025-05'. Defaults to current month.")] string? month,
        CancellationToken ct)
    {
        DateOnly from, to;

        if (month is not null)
        {
            var parts = month.Split('-');
            var year = int.Parse(parts[0]);
            var m = int.Parse(parts[1]);
            from = new DateOnly(year, m, 1);
            to = new DateOnly(year, m, DateTime.DaysInMonth(year, m));
        }
        else
        {
            var now = DateTime.Today;
            from = new DateOnly(now.Year, now.Month, 1);
            to = new DateOnly(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
        }

        var entries = await mediator.Send(new GetTimeEntriesQuery(from, to, null, true), ct);

        var summary = entries
            .GroupBy(e => new { e.ClientName, e.ProjectName })
            .Select(g => new
            {
                Client = g.Key.ClientName,
                Project = g.Key.ProjectName,
                TotalHours = Math.Round(g.Sum(e => e.DurationMinutes ?? 0) / 60.0, 2),
                EntryCount = g.Count(),
                InvoicedHours = Math.Round(g.Where(e => e.IsInvoiced).Sum(e => e.DurationMinutes ?? 0) / 60.0, 2)
            })
            .OrderBy(x => x.Client)
            .ThenBy(x => x.Project)
            .ToList();

        return new
        {
            Period = $"{from:yyyy-MM-dd} – {to:yyyy-MM-dd}",
            TotalHours = Math.Round(summary.Sum(x => x.TotalHours), 2),
            ByClientProject = summary
        };
    }

    [McpServerTool(Name = "get_active_timer")]
    [Description("Returns the currently running timer, or null if no timer is active.")]
    public async Task<object?> GetActiveTimer(CancellationToken ct)
        => await mediator.Send(new GetActiveTimerQuery(), ct);

    [McpServerTool(Name = "start_timer")]
    [Description("Starts a new timer for the given client and project. Fails if a timer is already running or no matching project is found.")]
    public async Task<object> StartTimer(
        [Description("Client name (case-insensitive substring match)")] string clientName,
        [Description("Project name (case-insensitive substring match)")] string projectName,
        [Description("Optional description of the work being done")] string? description,
        CancellationToken ct)
    {
        var project = await ResolveProjectAsync(clientName, projectName, ct);
        return await mediator.Send(new StartTimerCommand(project.Id, description ?? string.Empty), ct);
    }

    [McpServerTool(Name = "stop_timer")]
    [Description("Stops the currently running timer and returns the completed time entry.")]
    public async Task<object> StopTimer(CancellationToken ct)
    {
        var active = await mediator.Send(new GetActiveTimerQuery(), ct)
            ?? throw new InvalidOperationException("Brak aktywnego timera do zatrzymania.");

        return await mediator.Send(new StopTimerCommand(active.Id), ct);
    }

    [McpServerTool(Name = "pause_timer")]
    [Description("Wstrzymuje aktualnie działający timer i zwraca wpis czasu.")]
    public async Task<object> PauseTimer(CancellationToken ct)
    {
        var active = await mediator.Send(new GetActiveTimerQuery(), ct)
            ?? throw new InvalidOperationException("Brak aktywnego timera do wstrzymania.");
        return await mediator.Send(new PauseTimerCommand(active.Id), ct);
    }

    [McpServerTool(Name = "resume_timer")]
    [Description("Wznawia wstrzymany timer i zwraca wpis czasu.")]
    public async Task<object> ResumeTimer(CancellationToken ct)
    {
        var active = await mediator.Send(new GetActiveTimerQuery(), ct)
            ?? throw new InvalidOperationException("Brak wstrzymanego timera do wznowienia.");
        return await mediator.Send(new ResumeTimerCommand(active.Id), ct);
    }

    [McpServerTool(Name = "create_manual_time_entry")]
    [Description("Dodaje ręczny wpis czasu dla klienta/projektu w podanym przedziale (ISO 8601 z offsetem, " +
                 "np. '2025-05-10T09:00:00+02:00').")]
    public async Task<object> CreateManualTimeEntry(
        [Description("Nazwa klienta (fragment, bez rozróżniania wielkości liter).")] string clientName,
        [Description("Nazwa projektu (fragment).")] string projectName,
        [Description("Początek, ISO 8601, np. '2025-05-10T09:00:00+02:00'.")] string startedAt,
        [Description("Koniec, ISO 8601, np. '2025-05-10T12:30:00+02:00'.")] string stoppedAt,
        [Description("Opis wykonanej pracy (opcjonalnie).")] string? description,
        CancellationToken ct)
    {
        var project = await ResolveProjectAsync(clientName, projectName, ct);
        return await mediator.Send(new CreateManualEntryCommand(
            project.Id,
            DateTimeOffset.Parse(startedAt),
            DateTimeOffset.Parse(stoppedAt),
            description ?? string.Empty), ct);
    }

    [McpServerTool(Name = "update_time_entry")]
    [Description("Aktualizuje istniejący wpis czasu (po GUID). Pola pominięte pozostają bez zmian.")]
    public async Task<object> UpdateTimeEntry(
        [Description("GUID wpisu z get_time_entries.")] Guid entryId,
        [Description("Nowy początek ISO 8601 (opcjonalnie).")] string? startedAt,
        [Description("Nowy koniec ISO 8601 (opcjonalnie).")] string? stoppedAt,
        [Description("Nowy opis (opcjonalnie).")] string? description,
        CancellationToken ct)
    {
        var entries = await mediator.Send(new GetTimeEntriesQuery(null, null, null, true), ct);
        var current = entries.FirstOrDefault(e => e.Id == entryId)
            ?? throw new InvalidOperationException("Nie znaleziono wpisu czasu o podanym GUID.");

        var newStart = startedAt is not null ? DateTimeOffset.Parse(startedAt) : current.StartedAt;
        var newStop = stoppedAt is not null ? DateTimeOffset.Parse(stoppedAt) : (current.StoppedAt ?? current.StartedAt);

        return await mediator.Send(new UpdateTimeEntryCommand(
            entryId, current.ProjectId, newStart, newStop, description ?? current.Description), ct);
    }

    [McpServerTool(Name = "delete_time_entry")]
    [Description("Usuwa wpis czasu po GUID (usunięcie miękkie).")]
    public async Task<object> DeleteTimeEntry(
        [Description("GUID wpisu z get_time_entries.")] Guid entryId,
        CancellationToken ct)
    {
        await mediator.Send(new DeleteTimeEntryCommand(entryId), ct);
        return new { deleted = true, entryId };
    }

    private async Task<ProjectDto> ResolveProjectAsync(string clientName, string projectName, CancellationToken ct)
    {
        var clients = await mediator.Send(new GetClientsQuery(), ct);
        return clients
            .Where(c => c.Name.Contains(clientName, StringComparison.OrdinalIgnoreCase))
            .SelectMany(c => c.Projects)
            .FirstOrDefault(p =>
                p.Name.Contains(projectName, StringComparison.OrdinalIgnoreCase) && p.IsActive)
            ?? throw new InvalidOperationException(
                $"Nie znaleziono aktywnego projektu '{projectName}' dla klienta '{clientName}'.");
    }
}
