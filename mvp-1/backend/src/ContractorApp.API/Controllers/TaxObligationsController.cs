using ContractorApp.Application.Features.TaxObligations.Commands.DeleteTaxPayment;
using ContractorApp.Application.Features.TaxObligations.Commands.RecordTaxPayment;
using ContractorApp.Application.Features.TaxObligations.Queries.GetTaxObligations;
using Microsoft.AspNetCore.Mvc;

namespace ContractorApp.API.Controllers;

/// <summary>
/// Tracker zobowiązań podatkowych: naliczone ZUS/PIT/VAT per miesiąc
/// oraz rejestr rzeczywistych wpłat — "co już zapłaciłem vs. co jeszcze muszę".
/// </summary>
public class TaxObligationsController : BaseApiController
{
    /// <summary>
    /// Zobowiązania za rok: naliczone kwoty (z kalkulatora) + zarejestrowane wpłaty per miesiąc.
    /// </summary>
    [HttpGet("{year:int}")]
    public async Task<IActionResult> Get(
        int year, [FromQuery] string? taxForm, [FromQuery] string? zusStage, CancellationToken ct)
        => Ok(await Mediator.Send(new GetTaxObligationsQuery(year, taxForm, zusStage), ct));

    /// <summary>Rejestruje wpłatę zobowiązania (ZUS, PIT, VAT) za dany miesiąc.</summary>
    [HttpPost("{year:int}/{month:int}/payments")]
    public async Task<IActionResult> RecordPayment(
        int year, int month, [FromBody] RecordPaymentRequest body, CancellationToken ct)
    {
        var id = await Mediator.Send(new RecordTaxPaymentCommand(
            year, month, body.Type, body.Amount, body.PaidAt, body.Notes), ct);
        return Ok(new { id });
    }

    /// <summary>Usuwa zarejestrowaną wpłatę.</summary>
    [HttpDelete("payments/{id:guid}")]
    public async Task<IActionResult> DeletePayment(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteTaxPaymentCommand(id), ct);
        return NoContent();
    }

    public record RecordPaymentRequest(
        string Type,
        decimal Amount,
        string? PaidAt,
        string? Notes);
}
