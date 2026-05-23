using ContractorApp.Application.Features.TimeEntries.Commands.CreateManualEntry;
using ContractorApp.Application.Features.TimeEntries.Commands.DeleteTimeEntry;
using ContractorApp.Application.Features.TimeEntries.Commands.StartTimer;
using ContractorApp.Application.Features.TimeEntries.Commands.StopTimer;
using ContractorApp.Application.Features.TimeEntries.Commands.UpdateTimeEntry;
using ContractorApp.Application.Features.TimeEntries.Queries.GetActiveTimer;
using ContractorApp.Application.Features.TimeEntries.Queries.GetTimeEntries;
using Microsoft.AspNetCore.Mvc;

namespace ContractorApp.API.Controllers;

[Route("api/v1/time-entries")]
public class TimeEntriesController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] Guid? projectId, [FromQuery] bool includeInvoiced = true,
        CancellationToken ct = default)
        => Ok(await Mediator.Send(new GetTimeEntriesQuery(from, to, projectId, includeInvoiced), ct));

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
        => Ok(await Mediator.Send(new GetActiveTimerQuery(), ct));

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartTimerCommand cmd, CancellationToken ct)
        => Ok(await Mediator.Send(cmd, ct));

    [HttpPost("{id:guid}/stop")]
    public async Task<IActionResult> Stop(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new StopTimerCommand(id), ct));

    [HttpPost("manual")]
    public async Task<IActionResult> CreateManual([FromBody] CreateManualEntryCommand cmd, CancellationToken ct)
        => Ok(await Mediator.Send(cmd, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTimeEntryCommand cmd, CancellationToken ct)
        => Ok(await Mediator.Send(cmd with { Id = id }, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteTimeEntryCommand(id), ct);
        return NoContent();
    }
}
