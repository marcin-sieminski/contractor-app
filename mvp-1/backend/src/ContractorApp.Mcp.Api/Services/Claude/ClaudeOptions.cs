namespace ContractorApp.Mcp.Api.Services.Claude;

public class ClaudeOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = "claude-haiku-4-5-20251001";
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 4096;
    public int MaxToolIterations { get; set; } = 5;
    public int TimeoutSeconds { get; set; } = 120;
    public string SystemPrompt { get; set; } = string.Empty;
}
