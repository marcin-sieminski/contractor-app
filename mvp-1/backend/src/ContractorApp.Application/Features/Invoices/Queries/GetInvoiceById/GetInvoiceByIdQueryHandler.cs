using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Invoices.Queries.GetInvoiceById;

public class GetInvoiceByIdQueryHandler : IRequestHandler<GetInvoiceByIdQuery, InvoiceDto?>
{
    private readonly IApplicationDbContext _db;

    public GetInvoiceByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<InvoiceDto?> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Client)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        return invoice?.ToDto();
    }
}
