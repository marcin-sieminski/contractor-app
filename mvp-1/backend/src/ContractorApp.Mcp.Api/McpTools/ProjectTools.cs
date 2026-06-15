using System.ComponentModel;
using ContractorApp.Application.Features.Clients.Queries.GetClients;
using MediatR;
using ModelContextProtocol.Server;

namespace ContractorApp.Mcp.Api.McpTools;

[McpServerToolType]
public class ProjectTools(ISender mediator)
{
    [McpServerTool(Name = "list_projects")]
    [Description(
        "Lista projektów wszystkich klientów (lub filtr po nazwie klienta): nazwa projektu, klient, " +
        "stawka godzinowa, waluta i czy aktywny. Przydatne do wskazania projektu przy starcie timera " +
        "lub generowaniu faktury.")]
    public async Task<object> ListProjects(
        [Description("Filtr nazwy klienta (fragment, bez rozróżniania wielkości liter).")] string? clientName,
        CancellationToken ct)
    {
        var clients = await mediator.Send(new GetClientsQuery(), ct);

        if (clientName is not null)
            clients = clients
                .Where(c => c.Name.Contains(clientName, StringComparison.OrdinalIgnoreCase))
                .ToList();

        return clients
            .SelectMany(c => c.Projects.Select(p => new
            {
                p.Id,
                p.Name,
                Client = c.Name,
                ClientId = c.Id,
                p.HourlyRate,
                p.Currency,
                p.IsActive
            }))
            .ToList();
    }
}
