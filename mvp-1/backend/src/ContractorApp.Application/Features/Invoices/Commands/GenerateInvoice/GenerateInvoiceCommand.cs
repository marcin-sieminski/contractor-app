using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Enums;
using MediatR;

namespace ContractorApp.Application.Features.Invoices.Commands.GenerateInvoice;

public record GenerateInvoiceCommand(
    Guid ClientId,
    List<Guid> TimeEntryIds,
    DateOnly IssueDate,
    VatTreatment VatTreatment,
    Currency Currency,
    int PaymentDays = 14) : IRequest<InvoiceDto>;
