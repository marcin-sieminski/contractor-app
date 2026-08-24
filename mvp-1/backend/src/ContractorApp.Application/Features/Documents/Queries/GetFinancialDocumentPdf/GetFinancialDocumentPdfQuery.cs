using ContractorApp.Application.Common.Interfaces;
using MediatR;

namespace ContractorApp.Application.Features.Documents.Queries.GetFinancialDocumentPdf;

/// <summary>Wydruk PDF zestawienia finansowego (rachunek wyników lub bilans uproszczony).</summary>
public record GetFinancialDocumentPdfQuery(int Year, string? Type, string? Granularity)
    : IRequest<DocumentFileDto>;

public class GetFinancialDocumentPdfQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDocumentPdfGenerator pdfGenerator)
    : IRequestHandler<GetFinancialDocumentPdfQuery, DocumentFileDto>
{
    public async Task<DocumentFileDto> Handle(GetFinancialDocumentPdfQuery request, CancellationToken ct)
    {
        var type = StatementKinds.ParseType(request.Type);
        var gran = StatementKinds.ParseGranularity(request.Granularity);

        var dto = await FinancialStatementBuilder.BuildAsync(
            db, currentUser.UserId, request.Year, type, gran, ct);

        var fileName = $"{StatementKinds.FileStem(type)}_{request.Year}_{StatementKinds.GranularityKey(gran)}.pdf";
        return new DocumentFileDto(fileName, "application/pdf", pdfGenerator.Generate(dto));
    }
}
