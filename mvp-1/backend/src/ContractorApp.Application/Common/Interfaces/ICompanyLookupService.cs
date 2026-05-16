namespace ContractorApp.Application.Common.Interfaces;

public interface ICompanyLookupService
{
    Task<CompanyLookupResult?> LookupByNipAsync(string nip, CancellationToken cancellationToken = default);
}

public record CompanyLookupResult(
    string Name,
    string Nip,
    string? Regon,
    string? Street,
    string? City,
    string? PostalCode,
    string Country = "PL");
