using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Entities;
using ContractorApp.Domain.ValueObjects;
using MediatR;

namespace ContractorApp.Application.Features.Clients.Commands.CreateClient;

public class CreateClientCommandHandler : IRequestHandler<CreateClientCommand, ClientDto>
{
    private readonly IApplicationDbContext _db;

    public CreateClientCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ClientDto> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        var nip = new Nip(request.Nip);

        var client = new Client
        {
            Name = request.Name,
            Nip = nip.Value,
            Regon = request.Regon,
            Street = request.Street,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country,
            IsEuVatPayer = request.IsEuVatPayer,
            IsVerified = false
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync(cancellationToken);

        return client.ToDto();
    }
}
