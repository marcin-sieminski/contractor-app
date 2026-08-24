using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractorApp.Infrastructure.Services.Ollama;

public class OllamaService
{
    private readonly HttpClient _http;
    private readonly OllamaOptions _opts;
    private readonly ILogger<OllamaService> _log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public OllamaService(HttpClient http, IOptions<OllamaOptions> opts, ILogger<OllamaService> log)
    {
        _http = http;
        _opts = opts.Value;
        _log = log;
    }

    public async Task<OllamaTagsResponse> GetModelsAsync(CancellationToken ct)
    {
        using var response = await _http.GetAsync("/api/tags", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(cancellationToken: ct)
            ?? new OllamaTagsResponse();
    }

    public async Task<OllamaChatResponse> ChatAsync(
        IReadOnlyList<OllamaMessage> messages,
        IReadOnlyList<OllamaTool>? tools,
        CancellationToken ct,
        string? modelOverride = null)
    {
        var req = new OllamaChatRequest
        {
            Model = !string.IsNullOrWhiteSpace(modelOverride) ? modelOverride : _opts.Model,
            Messages = messages.ToList(),
            Tools = tools?.Count > 0 ? tools.ToList() : null,
            Stream = false,
            Options = new OllamaRequestOptions { Temperature = _opts.Temperature },
            KeepAlive = string.IsNullOrWhiteSpace(_opts.KeepAlive) ? null : _opts.KeepAlive
        };

        _log.LogDebug("Calling Ollama /api/chat with {MessageCount} messages, {ToolCount} tools",
            req.Messages.Count, req.Tools?.Count ?? 0);

        using var response = await _http.PostAsJsonAsync("/api/chat", req, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _log.LogError("Ollama returned {Status}: {Body}", response.StatusCode, body);
            throw new HttpRequestException(
                $"Ollama API returned {(int)response.StatusCode}: {body}",
                null,
                response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Ollama returned empty response body.");

        return result;
    }

    /// <summary>
    /// Strumieniuje odpowiedź modelu (stream:true, NDJSON) — yielduje kolejne fragmenty treści.
    /// Bez narzędzi: przeznaczone do strumieniowania finalnej odpowiedzi po rozwiązaniu narzędzi.
    /// </summary>
    public async IAsyncEnumerable<string> StreamChatAsync(
        IReadOnlyList<OllamaMessage> messages,
        [EnumeratorCancellation] CancellationToken ct,
        string? modelOverride = null)
    {
        var req = new OllamaChatRequest
        {
            Model = !string.IsNullOrWhiteSpace(modelOverride) ? modelOverride : _opts.Model,
            Messages = messages.ToList(),
            Tools = null,
            Stream = true,
            Options = new OllamaRequestOptions { Temperature = _opts.Temperature },
            KeepAlive = string.IsNullOrWhiteSpace(_opts.KeepAlive) ? null : _opts.KeepAlive
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(req, options: JsonOpts)
        };
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line)) continue;

            OllamaChatResponse? chunk;
            try { chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line, JsonOpts); }
            catch { continue; }

            var content = chunk?.Message?.Content;
            if (!string.IsNullOrEmpty(content)) yield return content;
            if (chunk?.Done == true) break;
        }
    }
}
