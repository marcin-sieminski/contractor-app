using ContractorApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.TaxObligations.Commands.DeleteTaxPayment;

public class DeleteTaxPaymentCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<DeleteTaxPaymentCommand>
{
    public async Task Handle(DeleteTaxPaymentCommand request, CancellationToken ct)
    {
        var payment = await db.TaxPayments
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.UserId == currentUser.UserId, ct);

        if (payment is null) return;

        db.TaxPayments.Remove(payment);
        await db.SaveChangesAsync(ct);
    }
}
