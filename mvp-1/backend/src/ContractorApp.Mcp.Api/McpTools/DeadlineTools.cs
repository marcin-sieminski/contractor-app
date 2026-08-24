using System.ComponentModel;
using ContractorApp.Application.Features.Deadlines.GetUpcomingDeadlines;
using MediatR;
using ModelContextProtocol.Server;

namespace ContractorApp.Mcp.Api.McpTools;

[McpServerToolType]
public class DeadlineTools(ISender mediator)
{
    [McpServerTool(Name = "get_upcoming_deadlines")]
    [Description("Returns upcoming Polish tax and ZUS deadlines: VAT-7, JPK_V7M, PIT-5L quarterly advance, ZUS payments. Looks ahead 60 days by default.")]
    public async Task<object> GetUpcomingDeadlines(
        [Description("How many days ahead to look. Default 60.")] int daysAhead = 60,
        CancellationToken ct = default)
        => await mediator.Send(new GetUpcomingDeadlinesQuery(daysAhead), ct);
}
