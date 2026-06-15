using ContractorApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Conversations;

/// <summary>Usuwa rozmowę bieżącego użytkownika wraz z wiadomościami (kaskadowo).</summary>
public record DeleteConversationCommand(Guid Id) : IRequest;

public class DeleteConversationCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<DeleteConversationCommand>
{
    public async Task Handle(DeleteConversationCommand request, CancellationToken ct)
    {
        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.UserId == currentUser.UserId, ct);
        if (conversation is null) return;

        db.Conversations.Remove(conversation);
        await db.SaveChangesAsync(ct);
    }
}
