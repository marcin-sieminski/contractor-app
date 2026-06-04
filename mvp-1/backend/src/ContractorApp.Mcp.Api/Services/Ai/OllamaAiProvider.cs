using System.Net;
using System.Text.Json;
using ContractorApp.Infrastructure.Services.Ollama;
using Microsoft.Extensions.Options;

namespace ContractorApp.Mcp.Api.Services.Ai;

public class OllamaAiProvider : IAiChatProvider
{
    private readonly OllamaService _ollama;
    private readonly OllamaOptions _opts;
    private readonly ILogger<OllamaAiProvider> _log;

    public string ProviderKey => "ollama";

    public OllamaAiProvider(OllamaService ollama, IOptions<OllamaOptions> opts, ILogger<OllamaAiProvider> log)
    {
        _ollama = ollama;
        _opts = opts.Value;
        _log = log;
    }

    public Task<OllamaTagsResponse> GetModelsAsync(CancellationToken ct)
        => _ollama.GetModelsAsync(ct);

    public async Task<AiChatResult> ChatAsync(AiChatRequest request, McpToolRegistry registry, CancellationToken ct)
    {
        var messages = new List<OllamaMessage>();
        if (!string.IsNullOrWhiteSpace(_opts.SystemPrompt))
            messages.Add(new OllamaMessage { Role = "system", Content = _opts.SystemPrompt });

        foreach (var m in request.Messages)
        {
            var role = NormalizeRole(m.Role);
            if (role is null) continue;
            messages.Add(new OllamaMessage { Role = role, Content = m.Content });
        }

        var tools = registry.GetOllamaTools();
        var toolSummaries = new List<ToolCallSummary>();
        var usedModel = !string.IsNullOrWhiteSpace(request.ModelOverride) ? request.ModelOverride : _opts.Model;
        var toolsSupported = true;

        for (int iteration = 0; iteration < _opts.MaxToolIterations; iteration++)
        {
            OllamaChatResponse response;
            try
            {
                response = await _ollama.ChatAsync(messages, toolsSupported ? tools : null, ct, request.ModelOverride);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest && toolsSupported)
            {
                _log.LogWarning("Model {Model} does not support tools, retrying without. Error: {Err}", usedModel, ex.Message);
                toolsSupported = false;
                response = await _ollama.ChatAsync(messages, null, ct, request.ModelOverride);
            }

            var assistantMessage = response.Message;
            messages.Add(assistantMessage);

            var toolCalls = assistantMessage.ToolCalls;
            if (!toolsSupported || toolCalls is null || toolCalls.Count == 0)
                return new AiChatResult(assistantMessage.Content ?? string.Empty, toolSummaries, usedModel, toolsSupported);

            foreach (var call in toolCalls)
            {
                var name = call.Function.Name;
                var args = call.Function.Arguments;

                _log.LogInformation("Ollama tool call #{Iteration}: {Name}({Args})", iteration, name, args.GetRawText());

                var resultJson = await registry.InvokeAsync(name, args, request.RequestServices, ct);

                using var resultDoc = JsonDocument.Parse(resultJson);
                toolSummaries.Add(new ToolCallSummary(name, args.Clone(), resultDoc.RootElement.Clone()));

                messages.Add(new OllamaMessage { Role = "tool", Content = resultJson, ToolName = name });
            }
        }

        var last = messages.LastOrDefault(m => m.Role == "assistant");
        return new AiChatResult(
            last?.Content ?? "(Przekroczono limit wywołań narzędzi)",
            toolSummaries,
            usedModel,
            toolsSupported);
    }

    private static string? NormalizeRole(string role) => role?.ToLowerInvariant() switch
    {
        "user" => "user",
        "assistant" => "assistant",
        "system" => "system",
        _ => null
    };
}
