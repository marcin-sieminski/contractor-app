using ContractorApp.Application.DTOs;
using MediatR;

namespace ContractorApp.Application.Features.Invoices.Commands.SubmitToKsef;

public record SubmitToKsefCommand(Guid InvoiceId, string SellerNip) : IRequest<InvoiceDto>;
