using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Entities;
using ContractorApp.Domain.Enums;
using ContractorApp.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Settlements.Commands.SaveAnnualSettlement;

public class SaveAnnualSettlementCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<SaveAnnualSettlementCommand, AnnualSettlementDto>
{
    public async Task<AnnualSettlementDto> Handle(SaveAnnualSettlementCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var form = SettlementForms.Parse(request.TaxForm);
        var stage = SettlementForms.ParseStage(request.ZusStage);

        var entity = await db.AnnualSettlements
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Year == request.Year && s.Form == form, ct);

        if (entity?.Status == SettlementStatus.Final)
            throw new DomainException(
                $"Rozliczenie {SettlementForms.Code(form)} za {request.Year} jest zatwierdzone. "
                + "Cofnij je do wersji roboczej, aby edytować.");

        if (entity is null)
        {
            entity = new AnnualSettlement { UserId = userId, Year = request.Year, Form = form };
            db.AnnualSettlements.Add(entity);
        }

        entity.RevenueOverride = request.RevenueOverride;
        entity.CostsOverride = request.CostsOverride;
        entity.ZusSocialPaid = Math.Max(0m, request.ZusSocialPaid);
        entity.ZusHealthPaid = Math.Max(0m, request.ZusHealthPaid);
        entity.TaxPrepaymentsPaid = Math.Max(0m, request.TaxPrepaymentsPaid);
        entity.IpBoxEnabled = request.IpBoxEnabled;
        entity.IpQualifyingPercent = Math.Clamp(request.IpQualifyingPercent, 0m, 100m);
        entity.NexusCoefficient = Math.Clamp(request.NexusCoefficient, 0m, 1m);

        if (request.Taxpayer is { } t)
        {
            entity.TaxpayerNip = Normalize(t.Nip)?.Replace("-", "").Replace(" ", "");
            entity.FirstName = Normalize(t.FirstName);
            entity.LastName = Normalize(t.LastName);
            entity.BirthDate = t.BirthDate;
            entity.Street = Normalize(t.Street);
            entity.BuildingNumber = Normalize(t.BuildingNumber);
            entity.ApartmentNumber = Normalize(t.ApartmentNumber);
            entity.PostalCode = Normalize(t.PostalCode);
            entity.City = Normalize(t.City);
            entity.TaxOfficeCode = Normalize(t.TaxOfficeCode);
            entity.Voivodeship = Normalize(t.Voivodeship);
            entity.County = Normalize(t.County);
            entity.Commune = Normalize(t.Commune);
        }

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return await SettlementAssembler.BuildAsync(db, userId, request.Year, form, stage, entity, ct);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
