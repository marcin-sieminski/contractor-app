using System.ComponentModel;
using ContractorApp.Application.Features.Documents.Queries.GetFinancialDocument;
using MediatR;
using ModelContextProtocol.Server;

namespace ContractorApp.Mcp.Api.McpTools;

[McpServerToolType]
public class DocumentTools(ISender mediator)
{
    [McpServerTool(Name = "get_financial_document")]
    [Description(
        "Zestawienie finansowe za rok: rachunek wyników (przychody/koszty/wynik) lub bilans uproszczony, " +
        "w granulacji miesięcznej/kwartalnej/rocznej. Używaj do pytań o rachunek zysków i strat oraz " +
        "stan majątkowy.")]
    public async Task<object> GetFinancialDocument(
        [Description("Rok. Domyślnie bieżący.")] int? year,
        [Description("Rodzaj: income_statement | balance_sheet. Domyślnie income_statement.")] string? type,
        [Description("Granulacja: monthly | quarterly | annual. Domyślnie monthly.")] string? granularity,
        CancellationToken ct)
        => await mediator.Send(new GetFinancialDocumentQuery(year ?? DateTime.Today.Year, type, granularity), ct);
}
