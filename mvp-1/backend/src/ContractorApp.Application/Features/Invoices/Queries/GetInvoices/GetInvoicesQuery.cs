using ContractorApp.Application.DTOs;
using MediatR;

namespace ContractorApp.Application.Features.Invoices.Queries.GetInvoices;

public record GetInvoicesQuery(Guid? ClientId = null) : IRequest<List<InvoiceDto>>;
