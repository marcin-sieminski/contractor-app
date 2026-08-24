using System.ComponentModel;
using ContractorApp.Application.Features.IpBox.Commands.SetProjectIpStatus;
using ContractorApp.Application.Features.IpBox.Commands.SetTimeEntryIpWork;
using ContractorApp.Application.Features.IpBox.Queries.GetIpBoxProgress;
using MediatR;
using ModelContextProtocol.Server;

namespace ContractorApp.Mcp.Api.McpTools;

[McpServerToolType]
public class IpBoxTools(ISender mediator)
{
    [McpServerTool(Name = "get_ip_box_progress")]
    [Description(
        "Postęp IP Box za rok: współczynnik Nexus, godziny IP vs łączne, przychód kwalifikowany, " +
        "szacowana oszczędność podatkowa (oraz prognoza całoroczna) i lista wpisów czasu wymagających " +
        "uzupełnienia statusu IP. Używaj do pytań o IP Box i potencjalne oszczędności.")]
    public async Task<object> GetIpBoxProgress(
        [Description("Rok. Domyślnie bieżący.")] int? year,
        CancellationToken ct)
        => await mediator.Send(new GetIpBoxProgressQuery(year ?? DateTime.Today.Year), ct);

    [McpServerTool(Name = "set_project_ip_status")]
    [Description("Oznacza projekt jako kwalifikujący się do IP Box (lub usuwa to oznaczenie). " +
                 "GUID projektu pobierz z list_projects.")]
    public async Task<object> SetProjectIpStatus(
        [Description("GUID projektu z list_projects.")] Guid projectId,
        [Description("true = projekt IP, false = zwykły.")] bool isIpProject,
        CancellationToken ct)
    {
        await mediator.Send(new SetProjectIpStatusCommand(projectId, isIpProject), ct);
        return new { updated = true, projectId, isIpProject };
    }

    [McpServerTool(Name = "set_time_entry_ip_work")]
    [Description("Oznacza wpis czasu jako pracę IP (lub usuwa oznaczenie), z opcjonalnym opisem prac " +
                 "rozwojowych. GUID wpisu pobierz z get_time_entries.")]
    public async Task<object> SetTimeEntryIpWork(
        [Description("GUID wpisu z get_time_entries.")] Guid timeEntryId,
        [Description("true = praca IP, false = zwykła.")] bool isIpWork,
        [Description("Opis pracy IP / prac rozwojowych (opcjonalnie).")] string? ipWorkDescription,
        CancellationToken ct)
    {
        await mediator.Send(new SetTimeEntryIpWorkCommand(timeEntryId, isIpWork, ipWorkDescription), ct);
        return new { updated = true, timeEntryId, isIpWork };
    }
}
