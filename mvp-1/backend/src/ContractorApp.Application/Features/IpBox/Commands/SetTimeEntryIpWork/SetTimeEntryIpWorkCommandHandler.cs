using ContractorApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.IpBox.Commands.SetTimeEntryIpWork;

public class SetTimeEntryIpWorkCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<SetTimeEntryIpWorkCommand>
{
    public async Task Handle(SetTimeEntryIpWorkCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var entry = await db.TimeEntries
            .Include(te => te.Project)
            .ThenInclude(p => p.Client)
            .FirstOrDefaultAsync(te => te.Id == request.TimeEntryId && te.Project.Client.UserId == userId, ct)
            ?? throw new KeyNotFoundException($"TimeEntry {request.TimeEntryId} not found.");

        entry.IsIpWork = request.IsIpWork;
        entry.IpWorkDescription = request.IpWorkDescription;
        entry.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
