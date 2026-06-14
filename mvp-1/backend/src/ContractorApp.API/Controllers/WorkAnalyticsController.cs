using ContractorApp.Application.Features.WorkAnalytics.Queries.GetWorkAnalytics;
using Microsoft.AspNetCore.Mvc;

namespace ContractorApp.API.Controllers;

[Route("api/v1/work-analytics")]
public class WorkAnalyticsController : BaseApiController
{
    /// <summary>
    /// Analiza czasu pracy: billable%, heatmap per dzień, trend miesięczny, alerty niefakturowanych wpisów.
    /// </summary>
    [HttpGet("{year:int}")]
    public async Task<IActionResult> Get(int year, CancellationToken ct)
        => Ok(await Mediator.Send(new GetWorkAnalyticsQuery(year), ct));
}
