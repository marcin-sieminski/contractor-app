using ContractorApp.Application.DTOs;
using MediatR;

namespace ContractorApp.Application.Features.Clients.Queries.GetClients;

public record GetClientsQuery : IRequest<List<ClientDto>>;
