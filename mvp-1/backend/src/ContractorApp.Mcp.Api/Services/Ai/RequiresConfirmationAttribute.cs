namespace ContractorApp.Mcp.Api.Services.Ai;

/// <summary>
/// Oznacza narzędzie MCP jako wymagające jawnego potwierdzenia użytkownika przed wykonaniem
/// (operacje nieodwracalne lub o skutkach prawnych, np. wystawienie faktury, wysyłka do KSeF).
/// Pętla tool-use w czacie nie wykona takiego narzędzia automatycznie — zatrzyma się i zwróci
/// <see cref="PendingActionInfo"/> do zatwierdzenia. Dla zewnętrznych hostów MCP potwierdzenie
/// zapewnia natywny UI zgody hosta.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequiresConfirmationAttribute(string actionDescription) : Attribute
{
    public string ActionDescription { get; } = actionDescription;
}
