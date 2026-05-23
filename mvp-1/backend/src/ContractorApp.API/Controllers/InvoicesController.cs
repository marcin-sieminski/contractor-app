using ContractorApp.Application.Features.Invoices.Commands.GenerateInvoice;
using ContractorApp.Application.Features.Invoices.Commands.SubmitToKsef;
using ContractorApp.Application.Features.Invoices.Commands.UpdateInvoice;
using ContractorApp.Application.Features.Invoices.Queries.GetInvoiceById;
using ContractorApp.Application.Features.Invoices.Queries.GetInvoices;
using ContractorApp.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ContractorApp.Infrastructure.Services.KSeF;

namespace ContractorApp.API.Controllers;

public class InvoicesController : BaseApiController
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly KsefOptions _ksefOpts;

    public InvoicesController(IApplicationDbContext db, ICurrentUserService currentUser, IOptions<KsefOptions> ksefOpts)
    {
        _db = db;
        _currentUser = currentUser;
        _ksefOpts = ksefOpts.Value;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? clientId, CancellationToken ct)
        => Ok(await Mediator.Send(new GetInvoicesQuery(clientId), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var invoice = await Mediator.Send(new GetInvoiceByIdQuery(id), ct);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpGet("{id:guid}/xml")]
    public async Task<IActionResult> GetXml(Guid id, CancellationToken ct)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Client)
            .FirstOrDefaultAsync(i => i.Id == id && i.Client.UserId == _currentUser.UserId, ct);
        if (invoice is null) return NotFound();
        if (string.IsNullOrEmpty(invoice.XmlContent)) return NoContent();
        return Content(invoice.XmlContent, "application/xml");
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateInvoiceCommand cmd, CancellationToken ct)
    {
        var result = await Mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoiceCommand cmd, CancellationToken ct)
    {
        if (id != cmd.InvoiceId) return BadRequest();
        return Ok(await Mediator.Send(cmd, ct));
    }

    [HttpPost("{id:guid}/submit-ksef")]
    public async Task<IActionResult> SubmitToKsef(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new SubmitToKsefCommand(id, _ksefOpts.TestNip), ct);
        return Ok(result);
    }
}
