using System.Text;
using ContractorApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Settlements.Queries.GetSettlementXml;

/// <summary>
/// XML zeznania zgodny ze schemą MF — do ręcznego wgrania w eFormularzu
/// (klient-eformularz.mf.gov.pl) i autoryzacji po stronie MF.
/// </summary>
public record GetSettlementXmlQuery(int Year, string TaxForm, string? ZusStage)
    : IRequest<SettlementFileDto>;

public class GetSettlementXmlQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDeclarationXmlBuilderFactory builderFactory)
    : IRequestHandler<GetSettlementXmlQuery, SettlementFileDto>
{
    public async Task<SettlementFileDto> Handle(GetSettlementXmlQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var form = SettlementForms.Parse(request.TaxForm);
        var stage = SettlementForms.ParseStage(request.ZusStage);

        var entity = await db.AnnualSettlements
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Year == request.Year && s.Form == form, ct);
        var dto = await SettlementAssembler.BuildAsync(db, userId, request.Year, form, stage, entity, ct);

        var xml = builderFactory.Resolve(form, request.Year).Build(dto);

        return new SettlementFileDto(xml.FileName, "application/xml", Encoding.UTF8.GetBytes(xml.Content));
    }
}
