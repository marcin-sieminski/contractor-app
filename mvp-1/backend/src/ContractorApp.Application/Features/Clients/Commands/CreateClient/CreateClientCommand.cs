using ContractorApp.Application.DTOs;
using MediatR;

namespace ContractorApp.Application.Features.Clients.Commands.CreateClient;

public record CreateClientCommand(
    string Name,
    string Nip,
    string? Regon,
    string? Street,
    string? City,
    string? PostalCode,
    string Country,
    bool IsEuVatPayer) : IRequest<ClientDto>;
