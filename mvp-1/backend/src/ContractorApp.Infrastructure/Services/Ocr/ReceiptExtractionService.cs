using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Exceptions;
using Docnet.Core;
using Docnet.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ContractorApp.Infrastructure.Services.Ocr;

/// <summary>
/// Rozpoznaje treść paragonu/faktury ze zdjęcia za pomocą modelu wizyjnego (Claude domyślnie, Ollama jako fallback lokalny).
/// Jedno wywołanie: OCR + ekstrakcja danych + kategoryzacja → ścisły JSON.
/// </summary>
public class ReceiptExtractionService : IReceiptExtractionService
{
    private readonly HttpClient _http;
    private readonly ReceiptOcrOptions _opts;
    private readonly ILogger<ReceiptExtractionService> _log;

    public ReceiptExtractionService(HttpClient http, IOptions<ReceiptOcrOptions> opts, ILogger<ReceiptExtractionService> log)
    {
        _http = http;
        _opts = opts.Value;
        _log = log;
    }

    private const string Prompt = """
        Przeanalizuj zdjęcie polskiego paragonu lub faktury i wyodrębnij dane wydatku.
        Zwróć WYŁĄCZNIE obiekt JSON (bez markdown, bez komentarzy) o polach:
        {
          "date": "RRRR-MM-DD",            // data wystawienia/sprzedaży
          "vendorName": "nazwa sprzedawcy",
          "vendorNip": "NIP sprzedawcy (same cyfry) lub null",
          "grossAmount": liczba,            // BRUTTO/do zapłaty — etykiety: "RAZEM", "SUMA", "Do zapłaty", "Wartość brutto", "SUMA PLN"
          "netAmount": liczba lub null,     // NETTO — etykiety: "Wartość netto", "Netto", "Razem netto"
          "vatAmount": liczba lub null,     // VAT — etykiety: "w tym VAT", "Kwota VAT", "Podatek VAT", "VAT 23%"
          "currency": "PLN" | "EUR" | "USD" | "GBP" | "CHF",
          "receiptNumber": "numer dokumentu lub null",
          "isVatDeductible": true | false,  // czy dokument to faktura VAT z NIP nabywcy (umożliwia odliczenie)
          "category": jedna z: "Software","Hardware","Office","Training","Travel","Phone","Insurance","Accounting","Marketing","Other",
          "categoryConfidence": liczba 0..1
        }
        Zasady: liczby z kropką dziesiętną, bez separatora tysięcy i bez symbolu waluty.
        Kwoty: odczytaj brutto, netto i VAT z tabeli podsumowania (zwykle na dole dokumentu). Podaj każdą widoczną; brakującą zostaw null — zostanie wyliczona.
        Kategorie: Software=oprogramowanie/subskrypcje SaaS, Hardware=sprzęt komputerowy/elektronika,
        Office=biuro/wyposażenie/materiały, Training=szkolenia/kursy/książki, Travel=podróże/paliwo/hotele,
        Phone=telefon/internet, Insurance=ubezpieczenia, Accounting=księgowość/usługi prawne,
        Marketing=reklama/marketing, Other=pozostałe. Jeśli pole nieczytelne — użyj null.
        """;

    public async Task<ReceiptExtractionResult> ExtractAsync(
        byte[] image, string contentType, string provider, CancellationToken ct)
    {
        var (bytes, mediaType) = PrepareImage(image, contentType);
        var base64 = Convert.ToBase64String(bytes);

        var (rawJson, model) = provider.Equals("ollama", StringComparison.OrdinalIgnoreCase)
            ? await CallOllamaAsync(base64, ct)
            : await CallClaudeAsync(base64, mediaType, ct);

        return Parse(rawJson, provider.ToLowerInvariant(), model);
    }

    // ---- Claude (Anthropic.SDK, multimodal) ----
    private async Task<(string rawJson, string model)> CallClaudeAsync(string base64, string mediaType, CancellationToken ct)
    {
        var apiKey = !string.IsNullOrWhiteSpace(_opts.ApiKey)
            ? _opts.ApiKey
            : Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
              ?? throw new InvalidOperationException(
                  "Klucz API Claude nie jest skonfigurowany. Ustaw zmienną środowiskową ANTHROPIC_API_KEY lub ReceiptOcr:ApiKey.");

        var client = new AnthropicClient(apiKey);
        var parameters = new MessageParameters
        {
            Model = _opts.ClaudeModel,
            MaxTokens = _opts.MaxTokens,
            Temperature = (decimal?)_opts.Temperature,
            Messages =
            [
                new Message
                {
                    Role = RoleType.User,
                    Content =
                    [
                        new ImageContent
                        {
                            Source = new ImageSource
                            {
                                MediaType = mediaType,
                                Data = base64
                            }
                        },
                        new TextContent { Text = Prompt }
                    ]
                }
            ]
        };

        var response = await client.Messages.GetClaudeMessageAsync(parameters, null, ct);
        var text = string.Join("", response.Content.OfType<TextContent>().Select(b => b.Text));
        return (text, _opts.ClaudeModel);
    }

    // ---- Ollama (lokalny model wizyjny) ----
    private async Task<(string rawJson, string model)> CallOllamaAsync(string base64, CancellationToken ct)
    {
        var url = _opts.OllamaBaseUrl.TrimEnd('/') + "/api/chat";
        var body = new
        {
            model = _opts.OllamaVisionModel,
            stream = false,
            format = "json",
            options = new { temperature = _opts.Temperature },
            messages = new[]
            {
                new { role = "user", content = Prompt, images = new[] { base64 } }
            }
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync(url, body, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            // Limit HttpClient.Timeout (a nie anulowanie przez klienta) — najczęściej wolny "zimny start" modelu na CPU.
            throw new DomainException(
                $"Lokalny model '{_opts.OllamaVisionModel}' nie odpowiedział w czasie {_opts.TimeoutSeconds}s. " +
                "Pierwsze rozpoznanie bywa wolne (ładowanie modelu do pamięci) — spróbuj ponownie, " +
                "zwiększ ReceiptOcr:TimeoutSeconds, użyj GPU lub przełącz na providera Claude.");
        }

        using var _resp = response;
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            _log.LogError("Ollama vision zwróciło {Status}: {Body}", response.StatusCode, err);
            throw new HttpRequestException(
                $"Lokalny model wizyjny Ollama ({_opts.OllamaVisionModel}) zwrócił błąd {(int)response.StatusCode}. " +
                $"Upewnij się, że model jest pobrany (`ollama pull {_opts.OllamaVisionModel}`) i że Twoja wersja Ollama go obsługuje.");
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var content = doc.RootElement.TryGetProperty("message", out var msg)
            && msg.TryGetProperty("content", out var c) ? c.GetString() ?? "{}" : "{}";
        return (content, _opts.OllamaVisionModel);
    }

    // ---- Wspólne: PDF→obraz (1. strona), skalowanie, normalizacja do JPEG ----
    // Modele wizyjne (Claude/Ollama) przyjmują obrazy, nie PDF — dlatego PDF rasteryzujemy.
    private (byte[] bytes, string mediaType) PrepareImage(byte[] original, string contentType)
    {
        var isPdf = string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
        try
        {
            using Image image = isPdf ? RenderPdfFirstPage(original) : Image.Load(original);

            var longest = Math.Max(image.Width, image.Height);
            if (longest > _opts.MaxImageDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(_opts.MaxImageDimension, _opts.MaxImageDimension)
                }));
            }

            using var ms = new MemoryStream();
            image.SaveAsJpeg(ms, new JpegEncoder { Quality = 85 });
            return (ms.ToArray(), "image/jpeg");
        }
        catch (Exception ex)
        {
            if (isPdf)
            {
                _log.LogError(ex, "Nie udało się wyrenderować pliku PDF.");
                throw new DomainException(
                    "Nie udało się odczytać pliku PDF. Upewnij się, że nie jest uszkodzony ani zabezpieczony hasłem.");
            }
            _log.LogWarning(ex, "Nie udało się przetworzyć obrazu — wysyłam oryginał.");
            var media = string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType.ToLowerInvariant();
            return (original, media);
        }
    }

    private static readonly object PdfRenderLock = new();

    private static Image RenderPdfFirstPage(byte[] pdf)
    {
        // PDFium (Docnet) nie jest thread-safe — serializujemy dostęp.
        lock (PdfRenderLock)
        {
            using var docReader = DocLib.Instance.GetDocReader(pdf, new PageDimensions(2.0));
            using var pageReader = docReader.GetPageReader(0);
            var raw = pageReader.GetImage();
            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();

            var image = Image.LoadPixelData<Bgra32>(raw, width, height);
            image.Mutate(x => x.BackgroundColor(Color.White)); // render PDF ma przezroczyste tło
            return image;
        }
    }

    // ---- Parsowanie JSON z odpowiedzi modelu ----
    private ReceiptExtractionResult Parse(string raw, string provider, string model)
    {
        var json = StripFences(raw);
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var (gross, net, vat) = ReconcileAmounts(
                GetDecimal(root, "grossAmount"),
                GetDecimal(root, "netAmount"),
                GetDecimal(root, "vatAmount"));

            return new ReceiptExtractionResult(
                Date: ParseDate(GetString(root, "date")),
                VendorName: GetString(root, "vendorName"),
                VendorNip: DigitsOnly(GetString(root, "vendorNip")),
                GrossAmount: gross,
                NetAmount: net,
                VatAmount: vat,
                Currency: NormalizeCurrency(GetString(root, "currency")),
                ReceiptNumber: GetString(root, "receiptNumber"),
                IsVatDeductible: GetBool(root, "isVatDeductible"),
                Category: GetString(root, "category"),
                CategoryConfidence: GetDouble(root, "categoryConfidence"),
                RawJson: json,
                Provider: provider,
                Model: model);
        }
        catch (Exception ex)
        {
            // Każda nieoczekiwana struktura odpowiedzi modelu (zły JSON, pole jako obiekt/tablica itd.)
            // nie może wywrócić requestu — zwracamy pusty wynik, użytkownik uzupełni ręcznie.
            _log.LogWarning(ex, "Nie udało się sparsować odpowiedzi modelu: {Raw}", raw);
            return new ReceiptExtractionResult(
                null, null, null, null, null, null, null, null, null, null, null, raw, provider, model);
        }
    }

    // Jeśli model odczytał 2 z 3 kwot, brakującą wyliczamy deterministycznie (brutto = netto + VAT).
    private static (decimal? gross, decimal? net, decimal? vat) ReconcileAmounts(decimal? g, decimal? n, decimal? v)
    {
        if (g is null && n is not null && v is not null)
            g = decimal.Round(n.Value + v.Value, 2);
        else if (n is null && g is not null && v is not null && g.Value >= v.Value)
            n = decimal.Round(g.Value - v.Value, 2);
        else if (v is null && g is not null && n is not null && g.Value >= n.Value)
            v = decimal.Round(g.Value - n.Value, 2);
        return (g, n, v);
    }

    private static string StripFences(string s)
    {
        s = s.Trim();
        if (s.StartsWith("```"))
        {
            var firstNewline = s.IndexOf('\n');
            if (firstNewline >= 0) s = s[(firstNewline + 1)..];
            if (s.EndsWith("```")) s = s[..^3];
        }
        // Wytnij od pierwszego '{' do ostatniego '}' na wypadek dodatkowego tekstu.
        var start = s.IndexOf('{');
        var end = s.LastIndexOf('}');
        return start >= 0 && end > start ? s[start..(end + 1)] : s.Trim();
    }

    private static string? GetString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null) return null;
        var v = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
        return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    }

    private static decimal? GetDecimal(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el)) return null;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var d)) return d;
        if (el.ValueKind != JsonValueKind.String) return null; // model bywa zwraca obiekt/tablicę — ignorujemy
        var s = el.GetString()?.Replace(" ", "").Replace(",", ".");
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static double? GetDouble(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el)) return null;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out var d)) return d;
        if (el.ValueKind != JsonValueKind.String) return null;
        return double.TryParse(el.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static bool? GetBool(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null) return null;
        return el.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(el.GetString(), out var b) ? b : null,
            _ => null
        };
    }

    private static DateOnly? ParseDate(string? s) =>
        DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    private static string? DigitsOnly(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : new string(s.Where(char.IsDigit).ToArray()) is { Length: > 0 } r ? r : null;

    private static string? NormalizeCurrency(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var c = s.Trim().ToUpperInvariant();
        return c is "PLN" or "EUR" or "USD" or "GBP" or "CHF" ? c : null;
    }
}
