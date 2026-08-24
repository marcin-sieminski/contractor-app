using ContractorApp.Application.DTOs;
using MediatR;

namespace ContractorApp.Application.Features.Clients.Commands.UpdateClient;

public record UpdateClientCommand(
    Guid Id,
    string Name,
    string Nip,
    string? Regon,
    string? Street,
    string? City,
    string? PostalCode,
    string Country,
    bool IsEuVatPayer) : IRequest<ClientDto>;
