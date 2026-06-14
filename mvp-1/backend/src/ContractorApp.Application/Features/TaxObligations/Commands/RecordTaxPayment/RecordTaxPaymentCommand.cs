using MediatR;

namespace ContractorApp.Application.Features.TaxObligations.Commands.RecordTaxPayment;

public record RecordTaxPaymentCommand(
    int Year,
    int Month,
    string Type,
    decimal Amount,
    string? PaidAt,
    string? Notes) : IRequest<Guid>;
