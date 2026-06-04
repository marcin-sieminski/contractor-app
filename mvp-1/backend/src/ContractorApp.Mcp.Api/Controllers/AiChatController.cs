using ContractorApp.Mcp.Api.Services.Ai;
using ContractorApp.Mcp.Api.Services.Claude;
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
    private readonly OllamaAiProvider _ollama;
    private readonly ClaudeService _claude;
    private readonly McpToolRegistry _registry;
    private readonly OllamaOptions _ollamaOpts;
    private readonly ILogger<AiChatController> _log;

    public AiChatController(
        OllamaAiProvider ollama,
        ClaudeService claude,
        McpToolRegistry registry,
        IOptions<OllamaOptions> ollamaOpts,
        ILogger<AiChatController> log)
    {
        _ollama = ollama;
        _claude = claude;
        _registry = registry;
        _ollamaOpts = ollamaOpts.Value;
        _log = log;
    }

    public record ChatMessageDto(string Role, string Content);
    public record ChatRequest(List<ChatMessageDto> Messages, string? Model = null, string? Provider = null);
    public record ModelInfo(string Name, long Size, string Provider);
    public record ChatResponse(string Content, List<ToolCallSummary> ToolCalls, string Model, bool ToolsSupported);

    [HttpGet("models")]
    public async Task<IActionResult> GetModels(CancellationToken ct)
    {
        try
        {
            var tags = await _ollama.GetModelsAsync(ct);
            var ollamaModels = tags.Models.Select(m => new ModelInfo(m.Name, m.Size, "ollama"));
            var claudeModels = _claude.GetModels().Select(m => new ModelInfo(m, 0, "claude"));
            return Ok(new
            {
                models = ollamaModels.Concat(claudeModels),
                defaultModel = _ollamaOpts.Model,
                defaultProvider = "ollama"
            });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to fetch models");
            return StatusCode(503, new { error = "Nie można pobrać listy modeli z Ollama.", detail = ex.Message });
        }
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (request?.Messages is null || request.Messages.Count == 0)
            return BadRequest(new { error = "Pole 'messages' jest wymagane i nie może być puste." });

        var messages = request.Messages
            .Select(m => new AiMessage(m.Role, m.Content))
            .ToList();

        IAiChatProvider provider = request.Provider?.ToLowerInvariant() == "claude"
            ? _claude
            : _ollama;

        var aiRequest = new AiChatRequest(messages, request.Model, HttpContext.RequestServices);

        try
        {
            var result = await provider.ChatAsync(aiRequest, _registry, ct);
            return Ok(new ChatResponse(result.Content, result.ToolCalls, result.Model, result.ToolsSupported));
        }
        catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
        {
            _log.LogError(ex, "Cannot reach Ollama at {Url}", _ollamaOpts.BaseUrl);
            return StatusCode(503, new
            {
                error = "Nie można połączyć się z Ollama. Czy serwer Ollama jest uruchomiony? " +
                        $"Sprawdź: {_ollamaOpts.BaseUrl}",
                detail = ex.Message
            });
        }
        catch (HttpRequestException ex)
        {
            _log.LogError(ex, "HTTP error from AI provider {Provider}", request?.Provider ?? "ollama");
            return StatusCode(502, new { error = $"Błąd HTTP providera AI: {ex.Message}", detail = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API"))
        {
            _log.LogError(ex, "AI provider configuration error");
            return StatusCode(503, new { error = ex.Message });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _log.LogDebug("Chat request cancelled");
            return StatusCode(499);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(504, new
            {
                error = "Timeout przekroczony. Spróbuj z szybszym modelem (Claude Haiku lub Qwen2.5 7B)."
            });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "AI chat unexpected error");
            return StatusCode(500, new { error = "Błąd asystenta AI.", detail = ex.Message });
        }
    }
}
