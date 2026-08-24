using ContractorApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Settlements.Queries.GetAnnualSettlement;

public class GetAnnualSettlementQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetAnnualSettlementQuery, AnnualSettlementDto>
{
    public async Task<AnnualSettlementDto> Handle(GetAnnualSettlementQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var form = SettlementForms.Parse(request.TaxForm);
        var stage = SettlementForms.ParseStage(request.ZusStage);

        var entity = await db.AnnualSettlements
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Year == request.Year && s.Form == form, ct);

        return await SettlementAssembler.BuildAsync(db, userId, request.Year, form, stage, entity, ct);
    }
}
