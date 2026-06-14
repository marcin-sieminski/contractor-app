using ContractorApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.IpBox.Commands.SetProjectIpStatus;

public class SetProjectIpStatusCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<SetProjectIpStatusCommand>
{
    public async Task Handle(SetProjectIpStatusCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var project = await db.Projects
            .Include(p => p.Client)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.Client.UserId == userId, ct)
            ?? throw new KeyNotFoundException($"Project {request.ProjectId} not found.");

        project.IsIpProject = request.IsIpProject;
        project.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
