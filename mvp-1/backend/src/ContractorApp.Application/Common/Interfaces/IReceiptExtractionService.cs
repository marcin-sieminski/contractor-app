namespace ContractorApp.Application.Common.Interfaces;

/// <summary>
/// Wynik rozpoznania treści paragonu/faktury przez model wizyjny.
/// Wszystkie pola opcjonalne — model zwraca tylko to, co rozpozna.
/// </summary>
public record ReceiptExtractionResult(
    DateOnly? Date,
    string? VendorName,
    string? VendorNip,
    decimal? GrossAmount,
    decimal? NetAmount,
    decimal? VatAmount,
    string? Currency,
    string? ReceiptNumber,
    bool? IsVatDeductible,
    string? Category,
    double? CategoryConfidence,
    string RawJson,
    string Provider,
    string Model);

/// <summary>
/// Rozpoznaje treść obrazu paragonu/faktury (OCR + ekstrakcja + kategoryzacja) w jednym wywołaniu modelu wizyjnego.
/// </summary>
public interface IReceiptExtractionService
{
    /// <param name="provider">"claude" (domyślnie) lub "ollama".</param>
    Task<ReceiptExtractionResult> ExtractAsync(
        byte[] image, string contentType, string provider, CancellationToken ct);
}
