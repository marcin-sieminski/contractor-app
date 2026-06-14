using ContractorApp.Application.Features.CashFlow.Queries.GetCashFlowForecast;
using Microsoft.AspNetCore.Mvc;

namespace ContractorApp.API.Controllers;

[Route("api/v1/cash-flow")]
public class CashFlowController : BaseApiController
{
    /// <summary>
    /// Rolling cash flow 90 dni: oczekiwane wpływy z faktur vs. zobowiązania ZUS/PIT/VAT,
    /// dzień po dniu z semaforami i rekomendacją bufora podatkowego.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] decimal startingBalance = 0,
        [FromQuery] string? taxForm = null,
        [FromQuery] string? zusStage = null,
        CancellationToken ct = default)
        => Ok(await Mediator.Send(new GetCashFlowForecastQuery(startingBalance, taxForm, zusStage), ct));
}
