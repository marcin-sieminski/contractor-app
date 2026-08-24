using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Conversations;

/// <summary>Dopisuje pojedynczą wiadomość do rozmowy (jeśli należy do bieżącego użytkownika).</summary>
public record AppendMessageCommand(Guid ConversationId, string Role, string Content, string? ToolCallsJson)
    : IRequest;

public class AppendMessageCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<AppendMessageCommand>
{
    public async Task Handle(AppendMessageCommand request, CancellationToken ct)
    {
        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId && c.UserId == currentUser.UserId, ct);
        if (conversation is null) return; // nie zapisujemy do cudzej/nieistniejącej rozmowy

        db.ConversationMessages.Add(new ConversationMessage
        {
            ConversationId = conversation.Id,
            Role = request.Role,
            Content = request.Content,
            ToolCallsJson = request.ToolCallsJson,
        });
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
