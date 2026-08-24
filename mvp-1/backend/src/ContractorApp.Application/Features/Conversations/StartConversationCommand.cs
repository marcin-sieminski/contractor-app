using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Entities;
using MediatR;

namespace ContractorApp.Application.Features.Conversations;

/// <summary>Tworzy nową (pustą) rozmowę dla bieżącego użytkownika i zwraca jej identyfikator.</summary>
public record StartConversationCommand(string Title) : IRequest<Guid>;

public class StartConversationCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<StartConversationCommand, Guid>
{
    public async Task<Guid> Handle(StartConversationCommand request, CancellationToken ct)
    {
        var conversation = new Conversation
        {
            UserId = currentUser.UserId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Rozmowa" : request.Title.Trim(),
        };

        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(ct);
        return conversation.Id;
    }
}
