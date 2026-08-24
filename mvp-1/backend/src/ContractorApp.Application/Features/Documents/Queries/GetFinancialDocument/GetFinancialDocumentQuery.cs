using ContractorApp.Application.Common.Interfaces;
using MediatR;

namespace ContractorApp.Application.Features.Documents.Queries.GetFinancialDocument;

/// <summary>
/// Wylicza zestawienie finansowe (rachunek wyników lub bilans uproszczony) dla roku
/// w wybranej granulacji (miesięcznie / kwartalnie / rocznie) do podglądu na widoku.
/// </summary>
/// <param name="Year">Rok dokumentu.</param>
/// <param name="Type">Rodzaj: income_statement | balance_sheet. Domyślnie income_statement.</param>
/// <param name="Granularity">Granulacja: monthly | quarterly | annual. Domyślnie monthly.</param>
public record GetFinancialDocumentQuery(int Year, string? Type, string? Granularity)
    : IRequest<FinancialDocumentDto>;

public class GetFinancialDocumentQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetFinancialDocumentQuery, FinancialDocumentDto>
{
    public Task<FinancialDocumentDto> Handle(GetFinancialDocumentQuery request, CancellationToken ct) =>
        FinancialStatementBuilder.BuildAsync(
            db,
            currentUser.UserId,
            request.Year,
            StatementKinds.ParseType(request.Type),
            StatementKinds.ParseGranularity(request.Granularity),
            ct);
}
