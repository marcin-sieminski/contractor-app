using System.Text.Json;
using ContractorApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Conversations;

/// <summary>Pełna rozmowa (z wiadomościami) bieżącego użytkownika lub null, jeśli nie znaleziono.</summary>
public record GetConversationQuery(Guid Id) : IRequest<ConversationDetailDto?>;

public class GetConversationQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetConversationQuery, ConversationDetailDto?>
{
    public async Task<ConversationDetailDto?> Handle(GetConversationQuery request, CancellationToken ct)
    {
        var conversation = await db.Conversations
            .Where(c => c.Id == request.Id && c.UserId == currentUser.UserId)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.CreatedAt,
                c.UpdatedAt,
                Messages = c.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new { m.Role, m.Content, m.ToolCallsJson, m.CreatedAt })
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct);

        if (conversation is null) return null;

        var messages = conversation.Messages
            .Select(m => new ConversationMessageDto(m.Role, m.Content, ParseToolCalls(m.ToolCallsJson), m.CreatedAt))
            .ToList();

        return new ConversationDetailDto(
            conversation.Id, conversation.Title, conversation.CreatedAt, conversation.UpdatedAt, messages);
    }

    private static JsonElement? ParseToolCalls(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonDocument.Parse(json).RootElement.Clone(); }
        catch { return null; }
    }
}
