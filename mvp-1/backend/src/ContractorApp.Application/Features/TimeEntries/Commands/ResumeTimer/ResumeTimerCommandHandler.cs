using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.TimeEntries.Commands.ResumeTimer;

public class ResumeTimerCommandHandler : IRequestHandler<ResumeTimerCommand, TimeEntryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ResumeTimerCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TimeEntryDto> Handle(ResumeTimerCommand request, CancellationToken cancellationToken)
    {
        var entry = await _db.TimeEntries
            .Include(t => t.Project).ThenInclude(p => p.Client)
            .FirstOrDefaultAsync(t =>
                t.Id == request.TimeEntryId &&
                t.DeletedAt == null &&
                t.Project.Client.UserId == _currentUser.UserId,
                cancellationToken)
            ?? throw new DomainException($"Wpis czasu {request.TimeEntryId} nie istnieje.");

        if (!entry.IsPaused)
            throw new DomainException("Timer nie jest wstrzymany.");

        entry.Resume(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return entry.ToDto(entry.Project.Name, entry.Project.Client.Name);
    }
}
