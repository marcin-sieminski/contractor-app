using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Clients.Queries.GetClients;

public class GetClientsQueryHandler : IRequestHandler<GetClientsQuery, List<ClientDto>>
{
    private readonly IApplicationDbContext _db;

    public GetClientsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ClientDto>> Handle(GetClientsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Clients
            .Where(c => c.DeletedAt == null)
            .Include(c => c.Projects.Where(p => p.DeletedAt == null))
            .OrderBy(c => c.Name)
            .Select(c => c.ToDto())
            .ToListAsync(cancellationToken);
    }
}
