namespace ContractorApp.Application.DTOs;

public record ClientDto(
    Guid Id,
    string Name,
    string Nip,
    string? Regon,
    string? Street,
    string? City,
    string? PostalCode,
    string Country,
    bool IsVerified,
    bool IsEuVatPayer,
    List<ProjectDto> Projects);

public record ProjectDto(
    Guid Id,
    Guid ClientId,
    string Name,
    decimal HourlyRate,
    string Currency,
    bool IsActive);
