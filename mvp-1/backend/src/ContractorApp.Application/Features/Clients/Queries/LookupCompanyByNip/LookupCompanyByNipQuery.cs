using ContractorApp.Application.Common.Interfaces;
using MediatR;

namespace ContractorApp.Application.Features.Clients.Queries.LookupCompanyByNip;

public record LookupCompanyByNipQuery(string Nip) : IRequest<CompanyLookupResult?>;
