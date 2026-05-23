using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.TimeEntries.Commands.UpdateTimeEntry;

public class UpdateTimeEntryCommandHandler : IRequestHandler<UpdateTimeEntryCommand, TimeEntryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateTimeEntryCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TimeEntryDto> Handle(UpdateTimeEntryCommand request, CancellationToken cancellationToken)
    {
        if (request.StoppedAt <= request.StartedAt)
            throw new DomainException("Czas zakończenia musi być po czasie rozpoczęcia.");

        var entry = await _db.TimeEntries
            .Include(e => e.Project).ThenInclude(p => p.Client)
            .FirstOrDefaultAsync(e =>
                e.Id == request.Id &&
                e.DeletedAt == null &&
                e.Project.Client.UserId == _currentUser.UserId,
                cancellationToken)
            ?? throw new DomainException("Wpis nie istnieje.");

        if (entry.IsInvoiced)
            throw new DomainException("Nie można edytować zafakturowanego wpisu.");

        if (entry.IsRunning)
            throw new DomainException("Nie można edytować aktywnego timera.");

        var project = await _db.Projects
            .Include(p => p.Client)
            .FirstOrDefaultAsync(p =>
                p.Id == request.ProjectId &&
                p.DeletedAt == null &&
                p.Client.UserId == _currentUser.UserId,
                cancellationToken)
            ?? throw new DomainException($"Projekt {request.ProjectId} nie istnieje.");

        entry.ProjectId = request.ProjectId;
        entry.StartedAt = request.StartedAt;
        entry.StoppedAt = request.StoppedAt;
        entry.DurationMinutes = (int)Math.Round((request.StoppedAt - request.StartedAt).TotalMinutes);
        entry.Description = request.Description;

        await _db.SaveChangesAsync(cancellationToken);

        return entry.ToDto(project.Name, project.Client.Name);
    }
}
