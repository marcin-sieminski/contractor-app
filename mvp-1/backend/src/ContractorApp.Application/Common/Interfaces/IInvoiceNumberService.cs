namespace ContractorApp.Application.Common.Interfaces;

public interface IInvoiceNumberService
{
    Task<string> GenerateNextAsync(int year, int month, CancellationToken cancellationToken = default);
}
