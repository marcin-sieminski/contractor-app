using System.Text.Json;

namespace ContractorApp.Mcp.Api.Services.Ai;

public interface IAiChatProvider
{
    string ProviderKey { get; }
    Task<AiChatResult> ChatAsync(AiChatRequest request, McpToolRegistry registry, CancellationToken ct);
}

public record AiChatRequest(
    List<AiMessage> Messages,
    string? ModelOverride,
    IServiceProvider RequestServices);

public record AiMessage(string Role, string Content);

public record AiChatResult(
    string Content,
    List<ToolCallSummary> ToolCalls,
    string Model,
    bool ToolsSupported);

public record ToolCallSummary(string Name, JsonElement Args, JsonElement Result);
