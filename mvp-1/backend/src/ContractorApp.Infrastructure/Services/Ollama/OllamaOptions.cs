namespace ContractorApp.Infrastructure.Services.Ollama;

public class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "qwen2.5:7b";
    public double Temperature { get; set; } = 0.2;
    public int MaxToolIterations { get; set; } = 5;
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Jak długo Ollama trzyma model w pamięci po ostatnim requeście.
    /// Format: "30m", "1h", "-1" (zawsze), "0" (od razu wyładuj). Domyślnie 5m w Ollama.
    /// </summary>
    public string KeepAlive { get; set; } = "30m";

    public string SystemPrompt { get; set; } = string.Empty;
}
