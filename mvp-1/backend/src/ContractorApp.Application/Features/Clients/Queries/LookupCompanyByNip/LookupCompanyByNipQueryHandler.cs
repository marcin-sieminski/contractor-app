using ContractorApp.Application.Common.Interfaces;
using MediatR;

namespace ContractorApp.Application.Features.Clients.Queries.LookupCompanyByNip;

public class LookupCompanyByNipQueryHandler : IRequestHandler<LookupCompanyByNipQuery, CompanyLookupResult?>
{
    private readonly ICompanyLookupService _lookup;

    public LookupCompanyByNipQueryHandler(ICompanyLookupService lookup) => _lookup = lookup;

    public Task<CompanyLookupResult?> Handle(LookupCompanyByNipQuery request, CancellationToken cancellationToken)
        => _lookup.LookupByNipAsync(request.Nip, cancellationToken);
}
