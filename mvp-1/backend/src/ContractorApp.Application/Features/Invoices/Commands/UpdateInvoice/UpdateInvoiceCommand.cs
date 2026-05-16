using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Enums;
using MediatR;

namespace ContractorApp.Application.Features.Invoices.Commands.UpdateInvoice;

public record UpdateInvoiceCommand(
    Guid InvoiceId,
    DateOnly IssueDate,
    DateOnly ServiceDate,
    DateOnly DueDate,
    VatTreatment VatTreatment,
    Currency Currency,
    List<UpdateLineItemDto> LineItems) : IRequest<InvoiceDto>;

public record UpdateLineItemDto(
    int LineNumber,
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitPrice);
