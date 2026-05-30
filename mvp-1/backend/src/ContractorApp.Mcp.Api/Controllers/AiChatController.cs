using System.Net;
using System.Text.Json;
using ContractorApp.Mcp.Api.Services.Ai;
using ContractorApp.Infrastructure.Services.Ollama;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ContractorApp.Mcp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/ai")]
public class AiChatController : ControllerBase
{
    private readonly OllamaService _ollama;
    private readonly McpToolRegistry _registry;
    private readonly OllamaOptions _opts;
    private readonly ILogger<AiChatController> _log;

    public AiChatController(
        OllamaService ollama,
        McpToolRegistry registry,
        IOptions<OllamaOptions> opts,
        ILogger<AiChatController> log)
    {
        _ollama = ollama;
        _registry = registry;
        _opts = opts.Value;
        _log = log;
    }

    public record ChatMessageDto(string Role, string Content);
    public record ChatRequest(List<ChatMessageDto> Messages, string? Model = null);
    public record ToolCallSummary(string Name, JsonElement Args, JsonElement Result);
    public record ChatResponse(string Content, List<ToolCallSummary> ToolCalls, string Model, bool ToolsSupported);
    public record ModelInfo(string Name, long Size);

    [HttpGet("models")]
    public async Task<IActionResult> GetModels(CancellationToken ct)
    {
        try
        {
            var tags = await _ollama.GetModelsAsync(ct);
            var models = tags.Models.Select(m => new ModelInfo(m.Name, m.Size)).ToList();
            return Ok(new { models, defaultModel = _opts.Model });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to fetch Ollama models");
            return StatusCode(503, new { error = "Nie można pobrać listy modeli z Ollama.", detail = ex.Message });
        }
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (request?.Messages is null || request.Messages.Count == 0)
            return BadRequest(new { error = "Pole 'messages' jest wymagane i nie może być puste." });

        var messages = new List<OllamaMessage>();
        if (!string.IsNullOrWhiteSpace(_opts.SystemPrompt))
            messages.Add(new OllamaMessage { Role = "system", Content = _opts.SystemPrompt });

        foreach (var m in request.Messages)
        {
            var role = NormalizeRole(m.Role);
            if (role is null) continue;
            messages.Add(new OllamaMessage { Role = role, Content = m.Content });
        }

        var tools = _registry.GetOllamaTools();
        var toolSummaries = new List<ToolCallSummary>();
        var usedModel = !string.IsNullOrWhiteSpace(request.Model) ? request.Model : _opts.Model;
        var toolsSupported = true;

        try
        {
            for (int iteration = 0; iteration < _opts.MaxToolIterations; iteration++)
            {
                OllamaChatResponse response;
                try
                {
                    response = await _ollama.ChatAsync(messages, toolsSupported ? tools : null, ct, request.Model);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest && toolsSupported)
                {
                    // Model doesn't support tool calling — retry without tools
                    _log.LogWarning("Model {Model} does not support tools, retrying without. Error: {Err}", usedModel, ex.Message);
                    toolsSupported = false;
                    response = await _ollama.ChatAsync(messages, null, ct, request.Model);
                }

                var assistantMessage = response.Message;
                messages.Add(assistantMessage);

                var toolCalls = assistantMessage.ToolCalls;
                if (!toolsSupported || toolCalls is null || toolCalls.Count == 0)
                {
                    return Ok(new ChatResponse(assistantMessage.Content ?? string.Empty, toolSummaries, usedModel, toolsSupported));
                }

                foreach (var call in toolCalls)
                {
                    var name = call.Function.Name;
                    var args = call.Function.Arguments;

                    _log.LogInformation("Tool call #{Iter}: {Name}({Args})",
                        iteration, name, args.GetRawText());

                    var resultJson = await _registry.InvokeAsync(name, args, HttpContext.RequestServices, ct);

                    using var resultDoc = JsonDocument.Parse(resultJson);
                    toolSummaries.Add(new ToolCallSummary(name, args.Clone(), resultDoc.RootElement.Clone()));

                    messages.Add(new OllamaMessage
                    {
                        Role = "tool",
                        Content = resultJson,
                        ToolName = name
                    });
                }
            }

            var last = messages.LastOrDefault(m => m.Role == "assistant");
            return Ok(new ChatResponse(
                last?.Content ?? "(Przekroczono limit wywołań narzędzi)",
                toolSummaries,
                usedModel,
                toolsSupported));
        }
        catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
        {
            _log.LogError(ex, "Cannot reach Ollama at {Url}", _opts.BaseUrl);
            return StatusCode(503, new
            {
                error = "Nie można połączyć się z Ollama. Czy serwer Ollama jest uruchomiony? " +
                        $"Sprawdź: {_opts.BaseUrl}",
                detail = ex.Message
            });
        }
        catch (HttpRequestException ex)
        {
            _log.LogError(ex, "Ollama HTTP error");
            return StatusCode(502, new { error = $"Ollama zwróciło błąd: {ex.Message}", detail = ex.Message });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _log.LogDebug("Chat request cancelled by client for model {Model}", usedModel);
            return StatusCode(499);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(504, new
            {
                error = $"Timeout przekroczony ({_opts.TimeoutSeconds}s). " +
                        "Lokalny model potrzebuje więcej czasu lub utknął."
            });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "AiChat unexpected error");
            return StatusCode(500, new { error = "Błąd asystenta AI.", detail = ex.Message });
        }
    }

    private static string? NormalizeRole(string role) => role?.ToLowerInvariant() switch
    {
        "user" => "user",
        "assistant" => "assistant",
        "system" => "system",
        _ => null
    };
}
