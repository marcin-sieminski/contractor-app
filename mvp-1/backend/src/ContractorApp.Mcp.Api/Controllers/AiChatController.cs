using System.Text;
using System.Text.Json;
using ContractorApp.Application.Features.Conversations;
using ContractorApp.Mcp.Api.Services.Ai;
using ContractorApp.Mcp.Api.Services.Claude;
using ContractorApp.Infrastructure.Services.Ollama;
using MediatR;
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
    private readonly ISender _mediator;
    private readonly OllamaOptions _ollamaOpts;
    private readonly ILogger<AiChatController> _log;

    private static readonly JsonSerializerOptions ToolJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public AiChatController(
        OllamaAiProvider ollama,
        ClaudeService claude,
        McpToolRegistry registry,
        ISender mediator,
        IOptions<OllamaOptions> ollamaOpts,
        ILogger<AiChatController> log)
    {
        _ollama = ollama;
        _claude = claude;
        _registry = registry;
        _mediator = mediator;
        _ollamaOpts = ollamaOpts.Value;
        _log = log;
    }

    public record ChatMessageDto(string Role, string Content);
    public record ChatRequest(
        List<ChatMessageDto> Messages, string? Model = null, string? Provider = null, Guid? ConversationId = null);
    public record ModelInfo(string Name, long Size, string Provider);
    public record PendingActionDto(string Tool, string ActionDescription, JsonElement Args);
    public record ChatResponse(
        string Content, List<ToolCallSummary> ToolCalls, string Model, bool ToolsSupported,
        PendingActionDto? PendingAction, Guid? ConversationId = null);
    public record ConfirmedAction(string Tool, JsonElement Args);
    public record ConfirmActionRequest(
        List<ChatMessageDto> Messages, ConfirmedAction Action,
        string? Model = null, string? Provider = null, Guid? ConversationId = null);

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
            var pending = result.PendingAction is null
                ? null
                : new PendingActionDto(
                    result.PendingAction.Tool, result.PendingAction.ActionDescription, result.PendingAction.Args);

            var conversationId = await PersistTurnAsync(request, result, ct);

            return Ok(new ChatResponse(
                result.Content, result.ToolCalls, result.Model, result.ToolsSupported, pending, conversationId));
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

    /// <summary>
    /// Wykonuje akcję wcześniej zatrzymaną przez bramkę potwierdzenia (PendingAction) i zwraca
    /// opisowe podsumowanie wyniku. Wywoływane po kliknięciu „Wykonaj" w UI.
    /// </summary>
    [HttpPost("chat/confirm")]
    public async Task<IActionResult> ConfirmAction([FromBody] ConfirmActionRequest request, CancellationToken ct)
    {
        if (request?.Action is null || string.IsNullOrWhiteSpace(request.Action.Tool))
            return BadRequest(new { error = "Pole 'action' z nazwą narzędzia jest wymagane." });

        if (!_registry.RequiresConfirmation(request.Action.Tool))
            return BadRequest(new { error = $"Narzędzie '{request.Action.Tool}' nie wymaga potwierdzenia." });

        string resultJson;
        try
        {
            resultJson = await _registry.InvokeAsync(
                request.Action.Tool, request.Action.Args, HttpContext.RequestServices, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Confirmed action {Tool} failed", request.Action.Tool);
            return StatusCode(500, new { error = $"Nie udało się wykonać akcji: {ex.Message}" });
        }

        using var execDoc = JsonDocument.Parse(resultJson);
        var executedCall = new ToolCallSummary(
            request.Action.Tool, request.Action.Args.Clone(), execDoc.RootElement.Clone());

        var messages = (request.Messages ?? new List<ChatMessageDto>())
            .Select(m => new AiMessage(m.Role, m.Content))
            .ToList();
        messages.Add(new AiMessage("user",
            $"Akcja '{request.Action.Tool}' została zatwierdzona i wykonana. Wynik (JSON): {resultJson}. " +
            "Potwierdź użytkownikowi wykonanie i zwięźle podsumuj wynik po polsku."));

        IAiChatProvider provider = request.Provider?.ToLowerInvariant() == "claude" ? _claude : _ollama;
        var aiRequest = new AiChatRequest(messages, request.Model, HttpContext.RequestServices) { ToolsDisabled = true };

        try
        {
            var result = await provider.ChatAsync(aiRequest, _registry, ct);
            var toolCalls = new List<ToolCallSummary> { executedCall };
            toolCalls.AddRange(result.ToolCalls);

            if (request.ConversationId is { } convId)
            {
                try
                {
                    var toolJson = JsonSerializer.Serialize(toolCalls, ToolJsonOptions);
                    await _mediator.Send(new AppendMessageCommand(convId, "assistant", result.Content, toolJson), ct);
                }
                catch (Exception ex) { _log.LogWarning(ex, "Nie udało się zapisać podsumowania akcji."); }
            }

            return Ok(new ChatResponse(
                result.Content, toolCalls, result.Model, result.ToolsSupported, null, request.ConversationId));
        }
        catch (Exception ex)
        {
            // Akcja już się wykonała — nie gubimy tego faktu, nawet jeśli podsumowanie LLM zawiedzie.
            _log.LogWarning(ex, "Summary after confirmed action failed; returning raw result.");
            return Ok(new ChatResponse(
                $"Wykonano akcję „{request.Action.Tool}\". Nie udało się wygenerować opisu — wynik w szczegółach wywołania.",
                new List<ToolCallSummary> { executedCall },
                "(none)",
                true,
                null));
        }
    }

    /// <summary>Zapisuje turę (wiadomość użytkownika + odpowiedź asystenta), tworząc rozmowę gdy trzeba.</summary>
    private async Task<Guid?> PersistTurnAsync(ChatRequest request, AiChatResult result, CancellationToken ct)
    {
        try
        {
            var convId = request.ConversationId;
            if (convId is null)
            {
                var firstUser = request.Messages.FirstOrDefault(m => IsUser(m.Role))?.Content ?? "Rozmowa";
                var title = firstUser.Length > 80 ? firstUser[..80] : firstUser;
                convId = await _mediator.Send(new StartConversationCommand(title), ct);
            }

            var lastUser = request.Messages.LastOrDefault(m => IsUser(m.Role))?.Content;
            if (!string.IsNullOrWhiteSpace(lastUser))
                await _mediator.Send(new AppendMessageCommand(convId.Value, "user", lastUser, null), ct);

            var toolJson = result.ToolCalls.Count > 0 ? JsonSerializer.Serialize(result.ToolCalls, ToolJsonOptions) : null;
            await _mediator.Send(new AppendMessageCommand(convId.Value, "assistant", result.Content, toolJson), ct);

            return convId;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Nie udało się zapisać historii rozmowy.");
            return request.ConversationId;
        }
    }

    private static bool IsUser(string role) => role.Equals("user", StringComparison.OrdinalIgnoreCase);

    [HttpGet("conversations")]
    public async Task<IActionResult> ListConversations(CancellationToken ct)
        => Ok(await _mediator.Send(new ListConversationsQuery(), ct));

    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> GetConversation(Guid id, CancellationToken ct)
    {
        var conversation = await _mediator.Send(new GetConversationQuery(id), ct);
        return conversation is null ? NotFound() : Ok(conversation);
    }

    [HttpDelete("conversations/{id:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteConversationCommand(id), ct);
        return NoContent();
    }

    /// <summary>Strumieniowy czat (SSE): zdarzenia delta | tool | pending_action | done | error.</summary>
    [HttpPost("chat/stream")]
    public async Task ChatStream([FromBody] ChatRequest request, CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        if (request?.Messages is null || request.Messages.Count == 0)
        {
            await WriteSseAsync("error", new { error = "Pole 'messages' jest wymagane i nie może być puste." }, ct);
            return;
        }

        var messages = request.Messages.Select(m => new AiMessage(m.Role, m.Content)).ToList();
        IAiChatProvider provider = request.Provider?.ToLowerInvariant() == "claude" ? _claude : _ollama;
        var aiRequest = new AiChatRequest(messages, request.Model, HttpContext.RequestServices);

        var contentSb = new StringBuilder();
        var toolCalls = new List<ToolCallSummary>();
        var model = request.Model ?? string.Empty;
        var toolsSupported = true;
        PendingActionInfo? pending = null;

        try
        {
            await foreach (var ev in provider.ChatStreamAsync(aiRequest, _registry, ct))
            {
                switch (ev)
                {
                    case AiTextDelta d:
                        contentSb.Append(d.Text);
                        await WriteSseAsync("delta", new { text = d.Text }, ct);
                        break;
                    case AiToolStarted t:
                        await WriteSseAsync("tool", new { name = t.Name }, ct);
                        break;
                    case AiPendingAction p:
                        pending = p.Action;
                        await WriteSseAsync("pending_action",
                            new PendingActionDto(p.Action.Tool, p.Action.ActionDescription, p.Action.Args), ct);
                        break;
                    case AiCompleted c:
                        model = c.Model;
                        toolsSupported = c.ToolsSupported;
                        toolCalls = c.ToolCalls;
                        if (contentSb.Length == 0) contentSb.Append(c.Content);
                        break;
                    case AiStreamError e:
                        await WriteSseAsync("error", new { error = e.Message }, ct);
                        return;
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Streaming chat failed");
            await WriteSseAsync("error", new { error = "Błąd asystenta AI.", detail = ex.Message }, ct);
            return;
        }

        Guid? conversationId = request.ConversationId;
        if (pending is null)
        {
            var result = new AiChatResult(contentSb.ToString(), toolCalls, model, toolsSupported);
            conversationId = await PersistTurnAsync(request, result, ct);
        }

        var pendingDto = pending is null
            ? null
            : new PendingActionDto(pending.Tool, pending.ActionDescription, pending.Args);

        await WriteSseAsync("done", new
        {
            conversationId,
            model,
            toolsSupported,
            toolCalls,
            pendingAction = pendingDto,
        }, ct);
    }

    private async Task WriteSseAsync(string eventName, object data, CancellationToken ct)
    {
        await Response.WriteAsync($"event: {eventName}\n", ct);
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(data, ToolJsonOptions)}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
