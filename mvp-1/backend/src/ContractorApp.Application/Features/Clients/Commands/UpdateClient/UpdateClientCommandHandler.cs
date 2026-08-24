using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Exceptions;
using ContractorApp.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Clients.Commands.UpdateClient;

public class UpdateClientCommandHandler : IRequestHandler<UpdateClientCommand, ClientDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateClientCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ClientDto> Handle(UpdateClientCommand request, CancellationToken cancellationToken)
    {
        var client = await _db.Clients
            .Include(c => c.Projects.Where(p => p.DeletedAt == null))
            .FirstOrDefaultAsync(
                c => c.Id == request.Id && c.UserId == _currentUser.UserId && c.DeletedAt == null,
                cancellationToken)
            ?? throw new DomainException("Klient nie istnieje.");

        var nip = new Nip(request.Nip);

        // NIP zmieniony ręcznie → status weryfikacji nieaktualny.
        if (client.Nip != nip.Value)
            client.IsVerified = false;

        client.Name = request.Name;
        client.Nip = nip.Value;
        client.Regon = request.Regon;
        client.Street = request.Street;
        client.City = request.City;
        client.PostalCode = request.PostalCode;
        client.Country = request.Country;
        client.IsEuVatPayer = request.IsEuVatPayer;
        client.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return client.ToDto();
    }
}
