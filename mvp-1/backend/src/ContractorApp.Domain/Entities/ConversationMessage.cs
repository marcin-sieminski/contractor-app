using ContractorApp.Domain.Common;

namespace ContractorApp.Domain.Entities;

/// <summary>
/// Pojedyncza wiadomość w rozmowie (rola: user | assistant). <see cref="ToolCallsJson"/> przechowuje
/// serializowane podsumowania wywołań narzędzi MCP, jeśli wiadomość asystenta ich użyła.
/// </summary>
public class ConversationMessage : Entity
{
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ToolCallsJson { get; set; }
}
