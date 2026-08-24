using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Enums;
using ContractorApp.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Settlements.Commands.ReopenAnnualSettlement;

/// <summary>Cofa zatwierdzone rozliczenie do wersji roboczej (czyści snapshot).</summary>
public record ReopenAnnualSettlementCommand(int Year, string TaxForm, string? ZusStage)
    : IRequest<AnnualSettlementDto>;

public class ReopenAnnualSettlementCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<ReopenAnnualSettlementCommand, AnnualSettlementDto>
{
    public async Task<AnnualSettlementDto> Handle(ReopenAnnualSettlementCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var form = SettlementForms.Parse(request.TaxForm);
        var stage = SettlementForms.ParseStage(request.ZusStage);

        var entity = await db.AnnualSettlements
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Year == request.Year && s.Form == form, ct)
            ?? throw new DomainException("Brak zapisanego rozliczenia dla tego roku i formy.");

        if (entity.Status != SettlementStatus.Final)
            throw new DomainException("Rozliczenie nie jest zatwierdzone.");

        entity.Status = SettlementStatus.Draft;
        entity.CalculationJson = null;
        entity.FinalizedAt = null;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return await SettlementAssembler.BuildAsync(db, userId, request.Year, form, stage, entity, ct);
    }
}
