using System.Text.Json;
using System.Text.Json.Serialization;

namespace ContractorApp.Infrastructure.Services.Ollama;

public class OllamaMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OllamaToolCall>? ToolCalls { get; set; }

    // Optional in Ollama responses, useful for echoing tool results back
    [JsonPropertyName("tool_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolName { get; set; }
}

public class OllamaToolCall
{
    [JsonPropertyName("function")]
    public OllamaFunctionCall Function { get; set; } = new();
}

public class OllamaFunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    // Ollama returns arguments as a JSON object in most cases, but spec allows string too.
    [JsonPropertyName("arguments")]
    public JsonElement Arguments { get; set; }
}

public class OllamaTool
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public OllamaFunctionDef Function { get; set; } = new();
}

public class OllamaFunctionDef
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public JsonElement Parameters { get; set; }
}

public class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OllamaMessage> Messages { get; set; } = new();

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OllamaTool>? Tools { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    [JsonPropertyName("options")]
    public OllamaRequestOptions? Options { get; set; }

    [JsonPropertyName("keep_alive")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? KeepAlive { get; set; }
}

public class OllamaRequestOptions
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }
}

public class OllamaChatResponse
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public OllamaMessage Message { get; set; } = new();

    [JsonPropertyName("done")]
    public bool Done { get; set; }
}

public class OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaModelInfo> Models { get; set; } = new();
}

public class OllamaModelInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }
}
