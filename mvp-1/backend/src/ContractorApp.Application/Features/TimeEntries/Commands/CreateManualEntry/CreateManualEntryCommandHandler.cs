using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Entities;
using ContractorApp.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.TimeEntries.Commands.CreateManualEntry;

public class CreateManualEntryCommandHandler : IRequestHandler<CreateManualEntryCommand, TimeEntryDto>
{
    private readonly IApplicationDbContext _db;

    public CreateManualEntryCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<TimeEntryDto> Handle(CreateManualEntryCommand request, CancellationToken cancellationToken)
    {
        if (request.StoppedAt <= request.StartedAt)
            throw new DomainException("Czas zakończenia musi być po czasie rozpoczęcia.");

        var project = await _db.Projects
            .Include(p => p.Client)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.DeletedAt == null, cancellationToken)
            ?? throw new DomainException($"Projekt {request.ProjectId} nie istnieje.");

        var entry = new TimeEntry
        {
            ProjectId = request.ProjectId,
            StartedAt = request.StartedAt,
            Description = request.Description
        };
        entry.Stop(request.StoppedAt);

        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        return entry.ToDto(project.Name, project.Client.Name);
    }
}
