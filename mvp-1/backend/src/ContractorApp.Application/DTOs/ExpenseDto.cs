namespace ContractorApp.Application.DTOs;

public record ExpenseDto(
    Guid Id,
    DateOnly Date,
    string Category,
    string Description,
    decimal Amount,
    string Currency,
    decimal? ExchangeRate,
    decimal AmountPLN,
    bool IsVatDeductible,
    string? ReceiptNumber,
    string? VendorName,
    string? VendorNip,
    decimal? NetAmount,
    decimal? VatAmount,
    Guid? ReceiptId,
    bool HasReceipt,
    DateTimeOffset CreatedAt);
