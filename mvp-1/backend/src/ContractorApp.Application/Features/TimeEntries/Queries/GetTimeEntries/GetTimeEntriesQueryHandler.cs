using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.TimeEntries.Queries.GetTimeEntries;

public class GetTimeEntriesQueryHandler : IRequestHandler<GetTimeEntriesQuery, List<TimeEntryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetTimeEntriesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<TimeEntryDto>> Handle(GetTimeEntriesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.TimeEntries
            .Include(t => t.Project).ThenInclude(p => p.Client)
            .Where(t => t.DeletedAt == null);

        if (request.ProjectId.HasValue)
            query = query.Where(t => t.ProjectId == request.ProjectId.Value);

        if (!request.IncludeInvoiced)
            query = query.Where(t => !t.IsInvoiced);

        if (request.From.HasValue)
            query = query.Where(t => DateOnly.FromDateTime(t.StartedAt.Date) >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(t => DateOnly.FromDateTime(t.StartedAt.Date) <= request.To.Value);

        return await query
            .OrderByDescending(t => t.StartedAt)
            .Select(t => t.ToDto(t.Project.Name, t.Project.Client.Name))
            .ToListAsync(cancellationToken);
    }
}
