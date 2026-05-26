using System.ComponentModel;
using ContractorApp.Application.Features.Invoices.Queries.GetFinancialSummary;
using ContractorApp.Application.Features.Invoices.Queries.GetInvoices;
using MediatR;
using ModelContextProtocol.Server;

namespace ContractorApp.API.McpTools;

[McpServerToolType]
public class InvoiceTools(ISender mediator)
{
    [McpServerTool(Name = "get_invoices")]
    [Description("Returns invoices for the authenticated user. Optionally filter by status (Draft/Submitted/Accepted/Rejected) and/or year.")]
    public async Task<object> GetInvoices(
        [Description("Status filter: Draft, Submitted, Accepted, or Rejected")] string? status,
        [Description("Year filter, e.g. 2025")] int? year,
        CancellationToken ct)
    {
        var all = await mediator.Send(new GetInvoicesQuery(), ct);

        if (status is not null)
            all = all.Where(i => i.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();

        if (year.HasValue)
            all = all.Where(i => i.IssueDate.Year == year.Value).ToList();

        return all;
    }

    [McpServerTool(Name = "get_financial_summary")]
    [Description("Returns a financial summary for the given year: total income (PLN), expenses, estimated PIT (19% linear), and ZUS estimate. Defaults to current year.")]
    public async Task<object> GetFinancialSummary(
        [Description("Year, e.g. 2025. Defaults to current year.")] int? year,
        CancellationToken ct)
        => await mediator.Send(new GetFinancialSummaryQuery(year), ct);
}
