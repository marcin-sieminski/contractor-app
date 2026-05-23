using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.TimeEntries.Queries.GetActiveTimer;

public class GetActiveTimerQueryHandler : IRequestHandler<GetActiveTimerQuery, TimeEntryDto?>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetActiveTimerQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TimeEntryDto?> Handle(GetActiveTimerQuery request, CancellationToken cancellationToken)
    {
        var entry = await _db.TimeEntries
            .Include(t => t.Project).ThenInclude(p => p.Client)
            .FirstOrDefaultAsync(t =>
                t.StoppedAt == null &&
                t.DeletedAt == null &&
                t.Project.Client.UserId == _currentUser.UserId,
                cancellationToken);

        return entry is null ? null : entry.ToDto(entry.Project.Name, entry.Project.Client.Name);
    }
}
