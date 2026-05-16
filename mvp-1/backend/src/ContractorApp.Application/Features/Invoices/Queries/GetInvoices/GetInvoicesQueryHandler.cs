using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Invoices.Queries.GetInvoices;

public class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, List<InvoiceDto>>
{
    private readonly IApplicationDbContext _db;

    public GetInvoicesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<InvoiceDto>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Invoices
            .Include(i => i.Client)
            .Include(i => i.LineItems)
            .AsQueryable();

        if (request.ClientId.HasValue)
            query = query.Where(i => i.ClientId == request.ClientId.Value);

        return await query
            .OrderByDescending(i => i.IssueDate)
            .Select(i => i.ToDto())
            .ToListAsync(cancellationToken);
    }
}
