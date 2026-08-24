using ContractorApp.Application.DTOs;
using MediatR;

namespace ContractorApp.Application.Features.Expenses.Commands.CreateExpense;

public record CreateExpenseCommand(
    DateOnly Date,
    string Category,
    string Description,
    decimal Amount,
    string Currency,
    decimal? ExchangeRate,
    bool IsVatDeductible,
    string? ReceiptNumber,
    string? VendorName = null,
    string? VendorNip = null,
    decimal? NetAmount = null,
    decimal? VatAmount = null,
    Guid? ReceiptId = null) : IRequest<ExpenseDto>;
