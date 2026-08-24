using System.Text.Json;

namespace ContractorApp.Application.Features.Conversations;

public record ConversationSummaryDto(
    Guid Id, string Title, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, int MessageCount);

public record ConversationMessageDto(
    string Role, string Content, JsonElement? ToolCalls, DateTimeOffset CreatedAt);

public record ConversationDetailDto(
    Guid Id, string Title, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, List<ConversationMessageDto> Messages);
