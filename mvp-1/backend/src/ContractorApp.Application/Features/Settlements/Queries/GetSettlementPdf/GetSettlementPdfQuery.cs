using ContractorApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Settlements.Queries.GetSettlementPdf;

/// <summary>Wydruk rozliczenia (PDF) z opisami pozycji i mapowaniem na formularz.</summary>
public record GetSettlementPdfQuery(int Year, string TaxForm, string? ZusStage)
    : IRequest<SettlementFileDto>;

public class GetSettlementPdfQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ISettlementPdfGenerator pdfGenerator)
    : IRequestHandler<GetSettlementPdfQuery, SettlementFileDto>
{
    public async Task<SettlementFileDto> Handle(GetSettlementPdfQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var form = SettlementForms.Parse(request.TaxForm);
        var stage = SettlementForms.ParseStage(request.ZusStage);

        var entity = await db.AnnualSettlements
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Year == request.Year && s.Form == form, ct);
        var dto = await SettlementAssembler.BuildAsync(db, userId, request.Year, form, stage, entity, ct);

        var fileName = $"Rozliczenie_{dto.FormCode.Replace("/", "")}_{dto.Year}.pdf";
        return new SettlementFileDto(fileName, "application/pdf", pdfGenerator.Generate(dto));
    }
}
