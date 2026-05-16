using ContractorApp.Application.DTOs;
using MediatR;

namespace ContractorApp.Application.Features.Invoices.Queries.GetInvoiceById;

public record GetInvoiceByIdQuery(Guid InvoiceId) : IRequest<InvoiceDto?>;
