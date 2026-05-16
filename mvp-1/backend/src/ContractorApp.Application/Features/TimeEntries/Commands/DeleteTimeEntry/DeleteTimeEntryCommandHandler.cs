using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.TimeEntries.Commands.DeleteTimeEntry;

public class DeleteTimeEntryCommandHandler : IRequestHandler<DeleteTimeEntryCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteTimeEntryCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteTimeEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.Id == request.TimeEntryId && t.DeletedAt == null, cancellationToken)
            ?? throw new DomainException($"Wpis czasu {request.TimeEntryId} nie istnieje.");

        if (entry.IsInvoiced)
            throw new DomainException("Nie można usunąć wpisu, który jest już na fakturze.");

        entry.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
