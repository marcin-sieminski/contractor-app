using ContractorApp.Domain.Common;

namespace ContractorApp.Domain.Entities;

/// <summary>Zapisana rozmowa użytkownika z asystentem AI (historia czatu).</summary>
public class Conversation : Entity
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<ConversationMessage> Messages { get; set; } = new();
}
