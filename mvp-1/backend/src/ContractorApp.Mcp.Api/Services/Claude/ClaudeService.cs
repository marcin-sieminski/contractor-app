using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using ContractorApp.Mcp.Api.Services.Ai;
using Microsoft.Extensions.Options;
using CommonTool = Anthropic.SDK.Common.Tool;
using CommonFunction = Anthropic.SDK.Common.Function;

namespace ContractorApp.Mcp.Api.Services.Claude;

public class ClaudeService : IAiChatProvider
{
    private readonly ClaudeOptions _opts;
    private readonly ILogger<ClaudeService> _log;

    private static readonly string[] KnownModels =
        ["claude-haiku-4-5-20251001", "claude-sonnet-4-6", "claude-opus-4-8"];

    public string ProviderKey => "claude";

    public IReadOnlyList<string> GetModels() => KnownModels;

    public ClaudeService(IOptions<ClaudeOptions> opts, ILogger<ClaudeService> log)
    {
        _opts = opts.Value;
        _log = log;
    }

    public async Task<AiChatResult> ChatAsync(AiChatRequest request, McpToolRegistry registry, CancellationToken ct)
    {
        var apiKey = !string.IsNullOrWhiteSpace(_opts.ApiKey)
            ? _opts.ApiKey
            : Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
              ?? throw new InvalidOperationException(
                  "Klucz API Claude nie jest skonfigurowany. Ustaw Claude:ApiKey w konfiguracji lub zmienną środowiskową ANTHROPIC_API_KEY.");

        var client = new AnthropicClient(apiKey);
        var model = !string.IsNullOrWhiteSpace(request.ModelOverride) ? request.ModelOverride : _opts.DefaultModel;

        var messages = BuildMessages(request.Messages);
        var tools = BuildClaudeTools(registry);
        var toolSummaries = new List<ToolCallSummary>();

        for (int iteration = 0; iteration < _opts.MaxToolIterations; iteration++)
        {
            var parameters = new MessageParameters
            {
                Model = model,
                Messages = messages,
                MaxTokens = _opts.MaxTokens,
                SystemMessage = _opts.SystemPrompt,
                Temperature = (decimal?)_opts.Temperature,
            };

            MessageResponse response;
            try
            {
                response = await client.Messages.GetClaudeMessageAsync(parameters, tools, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _log.LogError(ex, "Claude API call failed (iteration {Iteration}, model {Model})", iteration, model);
                throw;
            }

            var toolUseBlocks = response.Content.OfType<ToolUseContent>().ToList();

            if (toolUseBlocks.Count == 0 || response.StopReason == "end_turn")
            {
                var text = string.Join("", response.Content.OfType<TextContent>().Select(b => b.Text));
                return new AiChatResult(text, toolSummaries, model, true);
            }

            messages.Add(response.Message);

            var resultBlocks = new List<ContentBase>();
            foreach (var tu in toolUseBlocks)
            {
                _log.LogInformation("Claude tool call #{Iteration}: {Name}", iteration, tu.Name);

                var argsJson = tu.Input?.ToJsonString() ?? "{}";
                using var doc = JsonDocument.Parse(argsJson);
                var args = doc.RootElement.Clone();

                var resultJson = await registry.InvokeAsync(tu.Name, args, request.RequestServices, ct);

                using var resultDoc = JsonDocument.Parse(resultJson);
                toolSummaries.Add(new ToolCallSummary(tu.Name, args, resultDoc.RootElement.Clone()));

                resultBlocks.Add(new ToolResultContent
                {
                    ToolUseId = tu.Id,
                    Content = resultJson
                });
            }

            messages.Add(new Message
            {
                Role = RoleType.User,
                Content = resultBlocks
            });
        }

        var lastMsg = messages.LastOrDefault(m => m.Role == RoleType.Assistant);
        var lastText = lastMsg?.Content?.OfType<TextContent>().FirstOrDefault()?.Text
                       ?? "(Przekroczono limit wywołań narzędzi)";

        return new AiChatResult(lastText, toolSummaries, model, true);
    }

    private static List<Message> BuildMessages(List<AiMessage> aiMessages)
    {
        var result = new List<Message>();
        foreach (var m in aiMessages)
        {
            var role = m.Role.ToLowerInvariant();
            if (role == "user")
                result.Add(new Message(RoleType.User, m.Content));
            else if (role == "assistant")
                result.Add(new Message(RoleType.Assistant, m.Content));
        }
        return result;
    }

    private static List<CommonTool> BuildClaudeTools(McpToolRegistry registry)
    {
        return registry.GetProviderTools()
            .Select(pt =>
            {
                var function = new CommonFunction(pt.Name, pt.Description, pt.Schema);
                return new CommonTool(function);
            })
            .ToList();
    }
}
