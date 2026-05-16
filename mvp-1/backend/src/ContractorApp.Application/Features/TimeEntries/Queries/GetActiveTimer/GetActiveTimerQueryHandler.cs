using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.TimeEntries.Queries.GetActiveTimer;

public class GetActiveTimerQueryHandler : IRequestHandler<GetActiveTimerQuery, TimeEntryDto?>
{
    private readonly IApplicationDbContext _db;

    public GetActiveTimerQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<TimeEntryDto?> Handle(GetActiveTimerQuery request, CancellationToken cancellationToken)
    {
        var entry = await _db.TimeEntries
            .Include(t => t.Project).ThenInclude(p => p.Client)
            .FirstOrDefaultAsync(t => t.StoppedAt == null && t.DeletedAt == null, cancellationToken);

        return entry is null ? null : entry.ToDto(entry.Project.Name, entry.Project.Client.Name);
    }
}
