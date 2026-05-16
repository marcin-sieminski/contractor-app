namespace ContractorApp.Application.DTOs;

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    string ClientName,
    string ClientNip,
    DateOnly IssueDate,
    DateOnly ServiceDate,
    DateOnly DueDate,
    string Status,
    string VatTreatment,
    string Currency,
    decimal? ExchangeRate,
    DateOnly? ExchangeRateDate,
    string? ExchangeRateTableNumber,
    decimal TotalNet,
    decimal TotalVat,
    decimal TotalGross,
    string? KsefReferenceNumber,
    DateTimeOffset? KsefSubmittedAt,
    string? KsefError,
    List<LineItemDto> LineItems);

public record LineItemDto(
    int LineNumber,
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal VatRate,
    decimal NetAmount,
    decimal GrossAmount);
