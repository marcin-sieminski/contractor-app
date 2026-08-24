using ContractorApp.Application.Features.CurrencyExposure.Queries.GetCurrencyExposure;
using Microsoft.AspNetCore.Mvc;

namespace ContractorApp.API.Controllers;

[Route("api/v1/currency-exposure")]
public class CurrencyExposureController : BaseApiController
{
    /// <summary>
    /// Analiza ekspozycji walutowej: udział walut w przychodzie, trend miesięczny
    /// z kursami NBP, analiza wrażliwości na zmianę kursów EUR/USD/GBP/CHF.
    /// </summary>
    [HttpGet("{year:int}")]
    public async Task<IActionResult> Get(int year, CancellationToken ct)
        => Ok(await Mediator.Send(new GetCurrencyExposureQuery(year), ct));
}
