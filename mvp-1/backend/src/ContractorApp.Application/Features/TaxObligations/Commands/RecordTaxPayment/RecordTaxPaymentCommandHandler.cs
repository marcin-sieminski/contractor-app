using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Entities;
using ContractorApp.Domain.Enums;
using MediatR;

namespace ContractorApp.Application.Features.TaxObligations.Commands.RecordTaxPayment;

public class RecordTaxPaymentCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<RecordTaxPaymentCommand, Guid>
{
    public async Task<Guid> Handle(RecordTaxPaymentCommand request, CancellationToken ct)
    {
        var type = request.Type.Trim().ToLowerInvariant() switch
        {
            "pit" => TaxPaymentType.PIT,
            "zussocial" or "zus_social" => TaxPaymentType.ZusSocial,
            "zushealth" or "zus_health" => TaxPaymentType.ZusHealth,
            "vat" => TaxPaymentType.VAT,
            _ => throw new ArgumentException($"Nieznany typ wpłaty: {request.Type}")
        };

        DateOnly? paidAt = request.PaidAt is not null
            ? DateOnly.Parse(request.PaidAt)
            : null;

        var payment = new TaxPayment
        {
            UserId = currentUser.UserId,
            Year = request.Year,
            Month = request.Month,
            Type = type,
            Amount = request.Amount,
            PaidAt = paidAt,
            Notes = request.Notes
        };

        db.TaxPayments.Add(payment);
        await db.SaveChangesAsync(ct);
        return payment.Id;
    }
}
