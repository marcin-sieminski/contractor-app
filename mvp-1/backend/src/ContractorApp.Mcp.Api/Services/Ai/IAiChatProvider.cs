using System.Text.Json;

namespace ContractorApp.Mcp.Api.Services.Ai;

public interface IAiChatProvider
{
    string ProviderKey { get; }
    Task<AiChatResult> ChatAsync(AiChatRequest request, McpToolRegistry registry, CancellationToken ct);

    /// <summary>Strumieniowa wersja czatu — yielduje zdarzenia (delta tekstu, użycie narzędzia, akcja do potwierdzenia, zakończenie).</summary>
    IAsyncEnumerable<AiStreamEvent> ChatStreamAsync(AiChatRequest request, McpToolRegistry registry, CancellationToken ct);
}

/// <summary>Zdarzenie strumienia czatu.</summary>
public abstract record AiStreamEvent;

/// <summary>Fragment tekstu odpowiedzi (token/tokeny).</summary>
public sealed record AiTextDelta(string Text) : AiStreamEvent;

/// <summary>Asystent rozpoczął użycie narzędzia o podanej nazwie.</summary>
public sealed record AiToolStarted(string Name) : AiStreamEvent;

/// <summary>Akcja wymaga potwierdzenia — strumień się zatrzymał.</summary>
public sealed record AiPendingAction(PendingActionInfo Action) : AiStreamEvent;

/// <summary>Zakończenie: pełna treść, podsumowania narzędzi, model i czy narzędzia były wspierane.</summary>
public sealed record AiCompleted(string Content, List<ToolCallSummary> ToolCalls, string Model, bool ToolsSupported)
    : AiStreamEvent;

/// <summary>Błąd w trakcie strumieniowania.</summary>
public sealed record AiStreamError(string Message) : AiStreamEvent;

public record AiChatRequest(
    List<AiMessage> Messages,
    string? ModelOverride,
    IServiceProvider RequestServices)
{
    /// <summary>Gdy true, provider nie udostępnia modelowi narzędzi (np. do podsumowania po wykonanej akcji).</summary>
    public bool ToolsDisabled { get; init; }
}

public record AiMessage(string Role, string Content);

public record AiChatResult(
    string Content,
    List<ToolCallSummary> ToolCalls,
    string Model,
    bool ToolsSupported)
{
    /// <summary>Gdy ustawione, pętla zatrzymała się i oczekuje potwierdzenia akcji przez użytkownika.</summary>
    public PendingActionInfo? PendingAction { get; init; }
}

public record ToolCallSummary(string Name, JsonElement Args, JsonElement Result);

/// <summary>Akcja wymagająca potwierdzenia: nazwa narzędzia, opis dla użytkownika i argumenty do wykonania.</summary>
public record PendingActionInfo(string Tool, string ActionDescription, JsonElement Args);
