using ContractorApp.Application.Features.Analytics.Queries.GetFinancialForecast;
using Microsoft.AspNetCore.Mvc;

namespace ContractorApp.API.Controllers;

public class AnalyticsController : BaseApiController
{
    /// <summary>
    /// Prognoza finansowa na poszczególne miesiące roku (przychody, koszty, PIT, ZUS, VAT)
    /// na podstawie dotychczas zarejestrowanych faktur i kosztów.
    /// </summary>
    [HttpGet("forecast")]
    public async Task<IActionResult> GetForecast(
        [FromQuery] int? year,
        [FromQuery] string? taxForm,
        [FromQuery] string? zusStage,
        [FromQuery] decimal? vatRate,
        [FromQuery] bool? includeForecast,
        CancellationToken ct)
        => Ok(await Mediator.Send(new GetFinancialForecastQuery(year, taxForm, zusStage, vatRate, includeForecast), ct));
}
