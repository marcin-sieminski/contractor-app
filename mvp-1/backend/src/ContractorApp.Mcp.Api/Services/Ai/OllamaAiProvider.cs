using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
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

        IReadOnlyList<OllamaTool>? tools = request.ToolsDisabled ? null : registry.GetOllamaTools();
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

            var gated = toolCalls.FirstOrDefault(c => registry.RequiresConfirmation(c.Function.Name));
            if (gated is not null)
                return new AiChatResult(assistantMessage.Content ?? string.Empty, toolSummaries, usedModel, toolsSupported)
                {
                    PendingAction = new PendingActionInfo(
                        gated.Function.Name,
                        registry.GetConfirmationDescription(gated.Function.Name) ?? gated.Function.Name,
                        gated.Function.Arguments.Clone())
                };

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

    public async IAsyncEnumerable<AiStreamEvent> ChatStreamAsync(
        AiChatRequest request, McpToolRegistry registry, [EnumeratorCancellation] CancellationToken ct)
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

        IReadOnlyList<OllamaTool>? tools = request.ToolsDisabled ? null : registry.GetOllamaTools();
        var toolSummaries = new List<ToolCallSummary>();
        var usedModel = !string.IsNullOrWhiteSpace(request.ModelOverride) ? request.ModelOverride : _opts.Model;
        var toolsSupported = tools is not null;

        for (int iteration = 0; iteration < _opts.MaxToolIterations; iteration++)
        {
            OllamaChatResponse response;
            try
            {
                response = await _ollama.ChatAsync(messages, toolsSupported ? tools : null, ct, request.ModelOverride);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest && toolsSupported)
            {
                _log.LogWarning("Model {Model} does not support tools (stream), retrying without.", usedModel);
                toolsSupported = false;
                response = await _ollama.ChatAsync(messages, null, ct, request.ModelOverride);
            }

            var assistantMessage = response.Message;
            var toolCalls = assistantMessage.ToolCalls;

            if (!toolsSupported || toolCalls is null || toolCalls.Count == 0)
            {
                // Finalna odpowiedź — strumieniujemy tokeny prosto z modelu (stream:true, bez narzędzi).
                var sb = new StringBuilder();
                await foreach (var delta in _ollama.StreamChatAsync(messages, ct, request.ModelOverride))
                {
                    if (!string.IsNullOrEmpty(delta))
                    {
                        sb.Append(delta);
                        yield return new AiTextDelta(delta);
                    }
                }

                var finalText = sb.Length > 0 ? sb.ToString() : (assistantMessage.Content ?? string.Empty);
                yield return new AiCompleted(finalText, toolSummaries, usedModel, toolsSupported);
                yield break;
            }

            var gated = toolCalls.FirstOrDefault(c => registry.RequiresConfirmation(c.Function.Name));
            if (gated is not null)
            {
                yield return new AiPendingAction(new PendingActionInfo(
                    gated.Function.Name,
                    registry.GetConfirmationDescription(gated.Function.Name) ?? gated.Function.Name,
                    gated.Function.Arguments.Clone()));
                yield break;
            }

            messages.Add(assistantMessage);
            foreach (var call in toolCalls)
            {
                yield return new AiToolStarted(call.Function.Name);

                var resultJson = await registry.InvokeAsync(
                    call.Function.Name, call.Function.Arguments, request.RequestServices, ct);

                using var resultDoc = JsonDocument.Parse(resultJson);
                toolSummaries.Add(new ToolCallSummary(call.Function.Name, call.Function.Arguments.Clone(), resultDoc.RootElement.Clone()));

                messages.Add(new OllamaMessage { Role = "tool", Content = resultJson, ToolName = call.Function.Name });
            }
        }

        var last = messages.LastOrDefault(m => m.Role == "assistant");
        yield return new AiCompleted(
            last?.Content ?? "(Przekroczono limit wywołań narzędzi)", toolSummaries, usedModel, toolsSupported);
    }

    private static string? NormalizeRole(string role) => role?.ToLowerInvariant() switch
    {
        "user" => "user",
        "assistant" => "assistant",
        "system" => "system",
        _ => null
    };
}
