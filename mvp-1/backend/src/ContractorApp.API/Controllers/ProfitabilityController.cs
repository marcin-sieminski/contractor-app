using ContractorApp.Application.Features.Profitability.Queries.GetClientProfitability;
using Microsoft.AspNetCore.Mvc;

namespace ContractorApp.API.Controllers;

[Route("api/v1/profitability")]
public class ProfitabilityController : BaseApiController
{
    /// <summary>
    /// Rentowność per klient/projekt: przychód PLN, godziny billable, efektywna stawka/h, udział.
    /// </summary>
    [HttpGet("{year:int}")]
    public async Task<IActionResult> Get(int year, [FromQuery] int? month, CancellationToken ct)
        => Ok(await Mediator.Send(new GetClientProfitabilityQuery(year, month), ct));
}
