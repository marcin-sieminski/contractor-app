using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using ContractorApp.Infrastructure.Services.Ollama;
using ModelContextProtocol.Server;

namespace ContractorApp.Mcp.Api.Services.Ai;

/// <summary>
/// Skanuje klasy oznaczone [McpServerToolType] w bieżącym assembly i udostępnia je
/// dla lokalnego loop tool-use (np. z modelem Ollama). Wywołuje narzędzia bezpośrednio
/// przez DI, więc HttpContext użytkownika jest dostępny dla ICurrentUserService.
/// </summary>
public class McpToolRegistry
{
    private readonly List<ToolDescriptor> _tools;
    private readonly Dictionary<string, ToolDescriptor> _byName;
    private readonly List<OllamaTool> _ollamaTools;
    private readonly ILogger<McpToolRegistry> _log;

    public McpToolRegistry(ILogger<McpToolRegistry> log)
    {
        _log = log;
        _tools = DiscoverTools();
        _byName = _tools.ToDictionary(t => t.Name, StringComparer.Ordinal);
        _ollamaTools = _tools.Select(BuildOllamaTool).ToList();

        _log.LogInformation("McpToolRegistry: discovered {Count} tools: {Names}",
            _tools.Count, string.Join(", ", _tools.Select(t => t.Name)));
    }

    public IReadOnlyList<OllamaTool> GetOllamaTools() => _ollamaTools;

    public async Task<string> InvokeAsync(
        string toolName,
        JsonElement arguments,
        IServiceProvider scope,
        CancellationToken ct)
    {
        if (!_byName.TryGetValue(toolName, out var descriptor))
        {
            _log.LogWarning("Tool not found: {Name}", toolName);
            return JsonSerializer.Serialize(new { error = $"Unknown tool: {toolName}" });
        }

        // Ollama sometimes serializes arguments as a JSON string instead of an object.
        if (arguments.ValueKind == JsonValueKind.String)
        {
            var raw = arguments.GetString();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                try { arguments = JsonDocument.Parse(raw).RootElement.Clone(); }
                catch (JsonException) { /* keep as-is */ }
            }
        }

        try
        {
            var instance = scope.GetRequiredService(descriptor.DeclaringType);
            var args = MapArguments(descriptor.Method, arguments, ct);
            var rawResult = descriptor.Method.Invoke(instance, args);

            object? result = rawResult switch
            {
                Task t when t.GetType().IsGenericType => await UnwrapTask(t),
                Task t => await CompleteTask(t),
                _ => rawResult
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch (Exception ex)
        {
            var inner = ex is TargetInvocationException tie && tie.InnerException is not null
                ? tie.InnerException
                : ex;
            _log.LogError(inner, "Tool {Name} threw an exception", toolName);
            return JsonSerializer.Serialize(new { error = inner.Message });
        }
    }

    // ── Discovery ──────────────────────────────────────────────────────────────

    private static List<ToolDescriptor> DiscoverTools()
    {
        var asm = typeof(McpToolRegistry).Assembly;
        var result = new List<ToolDescriptor>();

        foreach (var type in asm.GetTypes())
        {
            if (type.GetCustomAttribute<McpServerToolTypeAttribute>() is null) continue;

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var toolAttr = method.GetCustomAttribute<McpServerToolAttribute>();
                if (toolAttr is null) continue;

                var name = toolAttr.Name ?? method.Name;
                var desc = method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;

                result.Add(new ToolDescriptor(name, desc, type, method));
            }
        }

        return result;
    }

    // ── JSON schema generation (OpenAI tools format) ────────────────────────────

    private static OllamaTool BuildOllamaTool(ToolDescriptor d)
    {
        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var p in d.Method.GetParameters())
        {
            if (p.ParameterType == typeof(CancellationToken)) continue;

            var prop = BuildPropertySchema(p);
            properties[p.Name!] = prop;

            if (IsRequired(p))
                required.Add(p.Name!);
        }

        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required
        };

        return new OllamaTool
        {
            Type = "function",
            Function = new OllamaFunctionDef
            {
                Name = d.Name,
                Description = d.Description,
                Parameters = JsonSerializer.SerializeToElement(schema)
            }
        };
    }

    private static JsonObject BuildPropertySchema(ParameterInfo p)
    {
        var (jsonType, format) = MapType(p.ParameterType);
        var desc = p.GetCustomAttribute<DescriptionAttribute>()?.Description;

        var obj = new JsonObject { ["type"] = jsonType };
        if (format is not null) obj["format"] = format;
        if (!string.IsNullOrWhiteSpace(desc)) obj["description"] = desc;
        return obj;
    }

    private static (string Type, string? Format) MapType(Type t)
    {
        var u = Nullable.GetUnderlyingType(t) ?? t;
        if (u == typeof(string)) return ("string", null);
        if (u == typeof(bool)) return ("boolean", null);
        if (u == typeof(int) || u == typeof(long) || u == typeof(short)) return ("integer", null);
        if (u == typeof(decimal) || u == typeof(double) || u == typeof(float)) return ("number", null);
        if (u == typeof(DateTime) || u == typeof(DateTimeOffset)) return ("string", "date-time");
        if (u == typeof(DateOnly)) return ("string", "date");
        if (u == typeof(Guid)) return ("string", "uuid");
        return ("string", null);
    }

    private static bool IsRequired(ParameterInfo p)
    {
        if (p.HasDefaultValue) return false;
        if (Nullable.GetUnderlyingType(p.ParameterType) is not null) return false;
        // Reference type with nullable annotation → not required
        if (!p.ParameterType.IsValueType)
        {
            var ctx = new NullabilityInfoContext();
            var info = ctx.Create(p);
            if (info.WriteState == NullabilityState.Nullable) return false;
        }
        return true;
    }

    // ── Argument mapping ───────────────────────────────────────────────────────

    private static object?[] MapArguments(MethodInfo method, JsonElement args, CancellationToken ct)
    {
        var parameters = method.GetParameters();
        var result = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            if (p.ParameterType == typeof(CancellationToken))
            {
                result[i] = ct;
                continue;
            }

            if (args.ValueKind == JsonValueKind.Object &&
                TryGetPropertyCaseInsensitive(args, p.Name!, out var value) &&
                value.ValueKind != JsonValueKind.Null)
            {
                result[i] = ConvertJsonValue(value, p.ParameterType);
            }
            else if (p.HasDefaultValue)
            {
                result[i] = p.DefaultValue;
            }
            else if (Nullable.GetUnderlyingType(p.ParameterType) is not null ||
                     !p.ParameterType.IsValueType)
            {
                result[i] = null;
            }
            else
            {
                throw new ArgumentException(
                    $"Missing required argument '{p.Name}' for method '{method.Name}'.");
            }
        }

        return result;
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement obj, string name, out JsonElement value)
    {
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private static object? ConvertJsonValue(JsonElement value, Type targetType)
    {
        var u = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (u == typeof(string)) return value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
        if (u == typeof(bool)) return value.ValueKind == JsonValueKind.True || (value.ValueKind == JsonValueKind.String && bool.Parse(value.GetString()!));
        if (u == typeof(int)) return value.ValueKind == JsonValueKind.Number ? value.GetInt32() : int.Parse(value.GetString()!);
        if (u == typeof(long)) return value.ValueKind == JsonValueKind.Number ? value.GetInt64() : long.Parse(value.GetString()!);
        if (u == typeof(decimal)) return value.ValueKind == JsonValueKind.Number ? value.GetDecimal() : decimal.Parse(value.GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        if (u == typeof(double)) return value.ValueKind == JsonValueKind.Number ? value.GetDouble() : double.Parse(value.GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        if (u == typeof(float)) return value.ValueKind == JsonValueKind.Number ? (float)value.GetDouble() : float.Parse(value.GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        if (u == typeof(DateTime)) return DateTime.Parse(value.GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        if (u == typeof(DateOnly)) return DateOnly.Parse(value.GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        if (u == typeof(Guid)) return Guid.Parse(value.GetString()!);

        // Fallback: deserialize as the target type
        return value.Deserialize(targetType);
    }

    private static async Task<object?> UnwrapTask(Task task)
    {
        await task.ConfigureAwait(false);
        var resultProp = task.GetType().GetProperty("Result");
        return resultProp?.GetValue(task);
    }

    private static async Task<object?> CompleteTask(Task task)
    {
        await task.ConfigureAwait(false);
        return null;
    }

    public record ToolDescriptor(string Name, string Description, Type DeclaringType, MethodInfo Method);
}
