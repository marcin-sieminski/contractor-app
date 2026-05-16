namespace ContractorApp.Application.Common.Interfaces;

public interface IKsefService
{
    Task<KsefSubmitResult> SubmitInvoiceAsync(string xmlContent, string sellerNip, CancellationToken cancellationToken = default);
}

public record KsefSubmitResult(bool Success, string? ReferenceNumber, string? Error);
