using System.ComponentModel;
using ContractorApp.Application.Features.Expenses.Queries.GetExpenses;
using MediatR;
using ModelContextProtocol.Server;

namespace ContractorApp.API.McpTools;

[McpServerToolType]
public class ExpenseTools(ISender mediator)
{
    [McpServerTool(Name = "get_expenses")]
    [Description("Returns expense records. Optionally filter by date range (ISO 8601) and/or category (Software/Hardware/Office/Training/Travel/Phone/Insurance/Accounting/Marketing/Other).")]
    public async Task<object> GetExpenses(
        [Description("Start date, e.g. '2025-01-01'")] string? from,
        [Description("End date, e.g. '2025-12-31'")] string? to,
        [Description("Category filter, e.g. 'Software'")] string? category,
        CancellationToken ct)
        => await mediator.Send(
            new GetExpensesQuery(
                from is not null ? DateOnly.Parse(from) : null,
                to is not null ? DateOnly.Parse(to) : null,
                category),
            ct);
}
