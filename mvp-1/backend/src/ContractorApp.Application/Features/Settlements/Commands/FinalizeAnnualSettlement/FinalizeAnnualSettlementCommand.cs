using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Enums;
using ContractorApp.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Settlements.Commands.FinalizeAnnualSettlement;

/// <summary>
/// Zatwierdza rozliczenie: waliduje dane podatnika i ostrzeżenia blokujące,
/// zamraża wynik w snapshocie. Po zatwierdzeniu edycja wymaga cofnięcia do roboczej.
/// </summary>
public record FinalizeAnnualSettlementCommand(int Year, string TaxForm, string? ZusStage)
    : IRequest<AnnualSettlementDto>;

public class FinalizeAnnualSettlementCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<FinalizeAnnualSettlementCommand, AnnualSettlementDto>
{
    public async Task<AnnualSettlementDto> Handle(FinalizeAnnualSettlementCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var form = SettlementForms.Parse(request.TaxForm);
        var stage = SettlementForms.ParseStage(request.ZusStage);

        var entity = await db.AnnualSettlements
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Year == request.Year && s.Form == form, ct)
            ?? throw new DomainException("Zapisz rozliczenie przed zatwierdzeniem.");

        if (entity.Status == SettlementStatus.Final)
            throw new DomainException("Rozliczenie jest już zatwierdzone.");

        ValidateTaxpayer(entity.TaxpayerNip, entity.FirstName, entity.LastName, entity.TaxOfficeCode);

        var dto = await SettlementAssembler.BuildAsync(db, userId, request.Year, form, stage, entity, ct);

        var blocking = dto.DataWarnings.FirstOrDefault(w => w.Contains("bez kursu"));
        if (blocking is not null)
            throw new DomainException("Nie można zatwierdzić rozliczenia: " + blocking);

        entity.CalculationJson = SettlementAssembler.SerializeSnapshot(dto);
        entity.Status = SettlementStatus.Final;
        entity.FinalizedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return await SettlementAssembler.BuildAsync(db, userId, request.Year, form, stage, entity, ct);
    }

    private static void ValidateTaxpayer(string? nip, string? firstName, string? lastName, string? taxOffice)
    {
        var missing = new List<string>();
        if (!SettlementForms.IsValidNip(nip)) missing.Add("poprawny NIP");
        if (string.IsNullOrWhiteSpace(firstName)) missing.Add("imię");
        if (string.IsNullOrWhiteSpace(lastName)) missing.Add("nazwisko");
        if (string.IsNullOrWhiteSpace(taxOffice) || taxOffice.Length != 4 || !taxOffice.All(char.IsDigit))
            missing.Add("czterocyfrowy kod urzędu skarbowego");

        if (missing.Count > 0)
            throw new DomainException(
                "Uzupełnij dane podatnika przed zatwierdzeniem: " + string.Join(", ", missing) + ".");
    }
}
