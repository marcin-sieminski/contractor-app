using ContractorApp.Application.Features.IpBox.Commands.SetProjectIpStatus;
using ContractorApp.Application.Features.IpBox.Commands.SetTimeEntryIpWork;
using ContractorApp.Application.Features.IpBox.Queries.ExportIpBoxCsv;
using ContractorApp.Application.Features.IpBox.Queries.GetIpBoxProgress;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractorApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/ip-box")]
public class IpBoxController(IMediator mediator) : ControllerBase
{
    [HttpGet("{year:int}")]
    public async Task<IActionResult> GetProgress(int year, CancellationToken ct)
    {
        var result = await mediator.Send(new GetIpBoxProgressQuery(year), ct);
        return Ok(result);
    }

    [HttpPut("projects/{id:guid}/ip-status")]
    public async Task<IActionResult> SetProjectIpStatus(Guid id, [FromBody] SetProjectIpStatusRequest body, CancellationToken ct)
    {
        await mediator.Send(new SetProjectIpStatusCommand(id, body.IsIpProject), ct);
        return NoContent();
    }

    [HttpPut("entries/{id:guid}/ip-work")]
    public async Task<IActionResult> SetTimeEntryIpWork(Guid id, [FromBody] SetTimeEntryIpWorkRequest body, CancellationToken ct)
    {
        await mediator.Send(new SetTimeEntryIpWorkCommand(id, body.IsIpWork, body.IpWorkDescription), ct);
        return NoContent();
    }

    [HttpGet("{year:int}/export-csv")]
    public async Task<IActionResult> ExportCsv(int year, [FromQuery] int? month, CancellationToken ct)
    {
        var data = await mediator.Send(new ExportIpBoxCsvQuery(year, month), ct);
        var filename = month.HasValue
            ? $"ipbox_{year}_{month:D2}.csv"
            : $"ipbox_{year}.csv";
        return File(data, "text/csv; charset=utf-8", filename);
    }
}

public record SetProjectIpStatusRequest(bool IsIpProject);
public record SetTimeEntryIpWorkRequest(bool IsIpWork, string? IpWorkDescription);
