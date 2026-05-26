using System.ComponentModel;
using ContractorApp.Application.Features.Clients.Queries.GetClients;
using ContractorApp.Application.Features.Clients.Queries.LookupCompanyByNip;
using MediatR;
using ModelContextProtocol.Server;

namespace ContractorApp.API.McpTools;

[McpServerToolType]
public class ClientTools(ISender mediator)
{
    [McpServerTool(Name = "get_clients")]
    [Description("Returns all clients with their active projects.")]
    public async Task<object> GetClients(CancellationToken ct)
        => await mediator.Send(new GetClientsQuery(), ct);

    [McpServerTool(Name = "lookup_company_by_nip")]
    [Description("Looks up a Polish company by NIP using the Biała Lista MF registry. Returns company name, address, and REGON. Spaces and dashes in NIP are ignored.")]
    public async Task<object?> LookupCompanyByNip(
        [Description("NIP number (10 digits, spaces and dashes are ignored)")] string nip,
        CancellationToken ct)
        => await mediator.Send(new LookupCompanyByNipQuery(nip), ct);
}
