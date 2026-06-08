using ContractorApp.Application.DTOs;
using MediatR;

namespace ContractorApp.Application.Features.Expenses.Commands.ScanReceipt;

public record ScanReceiptCommand(
    byte[] FileBytes,
    string FileName,
    string ContentType,
    string Provider) : IRequest<ScanReceiptResult>;
