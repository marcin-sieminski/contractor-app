using MediatR;

namespace ContractorApp.Application.Features.TaxObligations.Queries.GetTaxObligations;

public record GetTaxObligationsQuery(int Year, string? TaxForm, string? ZusStage)
    : IRequest<TaxObligationsDto>;
