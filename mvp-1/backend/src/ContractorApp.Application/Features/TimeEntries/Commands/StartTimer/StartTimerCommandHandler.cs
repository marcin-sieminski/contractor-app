using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Entities;
using ContractorApp.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.TimeEntries.Commands.StartTimer;

public class StartTimerCommandHandler : IRequestHandler<StartTimerCommand, TimeEntryDto>
{
    private readonly IApplicationDbContext _db;

    public StartTimerCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<TimeEntryDto> Handle(StartTimerCommand request, CancellationToken cancellationToken)
    {
        var activeTimer = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.StoppedAt == null && t.DeletedAt == null, cancellationToken);

        if (activeTimer is not null)
            throw new DomainException("Istnieje już uruchomiony timer. Zatrzymaj go przed rozpoczęciem nowego.");

        var project = await _db.Projects
            .Include(p => p.Client)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.DeletedAt == null, cancellationToken)
            ?? throw new DomainException($"Projekt {request.ProjectId} nie istnieje.");

        var entry = new TimeEntry
        {
            ProjectId = request.ProjectId,
            StartedAt = DateTimeOffset.UtcNow,
            Description = request.Description
        };

        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        return entry.ToDto(project.Name, project.Client.Name);
    }
}
