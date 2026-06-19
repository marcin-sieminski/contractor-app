using ContractorApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Conversations;

/// <summary>Lista rozmów bieżącego użytkownika (najnowsze pierwsze).</summary>
public record ListConversationsQuery : IRequest<IReadOnlyList<ConversationSummaryDto>>;

public class ListConversationsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ListConversationsQuery, IReadOnlyList<ConversationSummaryDto>>
{
    public async Task<IReadOnlyList<ConversationSummaryDto>> Handle(ListConversationsQuery request, CancellationToken ct)
        => await db.Conversations
            .Where(c => c.UserId == currentUser.UserId)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ConversationSummaryDto(c.Id, c.Title, c.CreatedAt, c.UpdatedAt, c.Messages.Count))
            .ToListAsync(ct);
}
