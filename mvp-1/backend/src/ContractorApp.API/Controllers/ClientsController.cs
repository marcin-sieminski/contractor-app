using ContractorApp.Application.Features.Clients.Commands.CreateClient;
using ContractorApp.Application.Features.Clients.Queries.GetClients;
using ContractorApp.Application.Features.Clients.Queries.LookupCompanyByNip;
using Microsoft.AspNetCore.Mvc;

namespace ContractorApp.API.Controllers;

public class ClientsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await Mediator.Send(new GetClientsQuery(), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClientCommand cmd, CancellationToken ct)
    {
        var result = await Mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup([FromQuery] string nip, CancellationToken ct)
    {
        var result = await Mediator.Send(new LookupCompanyByNipQuery(nip), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
