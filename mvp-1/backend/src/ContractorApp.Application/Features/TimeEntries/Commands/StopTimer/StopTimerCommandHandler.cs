using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.TimeEntries.Commands.StopTimer;

public class StopTimerCommandHandler : IRequestHandler<StopTimerCommand, TimeEntryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public StopTimerCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TimeEntryDto> Handle(StopTimerCommand request, CancellationToken cancellationToken)
    {
        var entry = await _db.TimeEntries
            .Include(t => t.Project).ThenInclude(p => p.Client)
            .FirstOrDefaultAsync(t =>
                t.Id == request.TimeEntryId &&
                t.DeletedAt == null &&
                t.Project.Client.UserId == _currentUser.UserId,
                cancellationToken)
            ?? throw new DomainException($"Wpis czasu {request.TimeEntryId} nie istnieje.");

        if (entry.StoppedAt != null)
            throw new DomainException("Ten timer już jest zatrzymany.");

        entry.Stop(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return entry.ToDto(entry.Project.Name, entry.Project.Client.Name);
    }
}
