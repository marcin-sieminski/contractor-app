using MediatR;

namespace ContractorApp.Application.Features.TaxObligations.Commands.DeleteTaxPayment;

public record DeleteTaxPaymentCommand(Guid Id) : IRequest;
