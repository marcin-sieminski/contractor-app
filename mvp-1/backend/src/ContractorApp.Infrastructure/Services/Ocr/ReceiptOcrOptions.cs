namespace ContractorApp.Infrastructure.Services.Ocr;

public class ReceiptOcrOptions
{
    /// <summary>Klucz Claude. Jeśli pusty — brany ze zmiennej środowiskowej ANTHROPIC_API_KEY.</summary>
    public string ApiKey { get; set; } = string.Empty;
    public string ClaudeModel { get; set; } = "claude-haiku-4-5-20251001";

    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string OllamaVisionModel { get; set; } = "qwen2.5vl";

    public int MaxTokens { get; set; } = 1024;
    public double Temperature { get; set; } = 0.0;

    /// <summary>Dłuższy bok obrazu jest skalowany do tej wartości przed wysłaniem do modelu (kontrola kosztu/tokenów).</summary>
    public int MaxImageDimension { get; set; } = 1568;

    public int TimeoutSeconds { get; set; } = 600;
}
