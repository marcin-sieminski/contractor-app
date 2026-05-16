using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ContractorApp.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ContractorApp.Infrastructure.Services.CompanyLookup;

/// <summary>
/// Wyszukuje dane firmy po NIP używając Białej Listy MF (wl-api.mf.gov.pl) — darmowe API, brak autoryzacji.
/// Fallback: CEIDG v3 (wymaga JWT, opcjonalne).
/// </summary>
public class CompanyLookupService : ICompanyLookupService
{
    private readonly HttpClient _http;
    private readonly ILogger<CompanyLookupService> _log;
    private readonly Dictionary<string, (CompanyLookupResult Result, DateTimeOffset CachedAt)> _cache = new();

    public CompanyLookupService(HttpClient http, ILogger<CompanyLookupService> log)
    {
        _http = http;
        _log = log;
    }

    public async Task<CompanyLookupResult?> LookupByNipAsync(string nip, CancellationToken cancellationToken = default)
    {
        var digits = new string(nip.Where(char.IsDigit).ToArray());

        if (_cache.TryGetValue(digits, out var cached) && DateTimeOffset.UtcNow - cached.CachedAt < TimeSpan.FromHours(24))
            return cached.Result;

        var result = await TryBialaListaAsync(digits, cancellationToken);

        if (result is not null)
            _cache[digits] = (result, DateTimeOffset.UtcNow);

        return result;
    }

    /// <summary>
    /// Biała Lista MF — https://wl-api.mf.gov.pl
    /// GET /api/search/nip/{nip}?date=YYYY-MM-DD
    /// Darmowe, bez autoryzacji, oficjalne API Ministerstwa Finansów.
    /// </summary>
    private async Task<CompanyLookupResult?> TryBialaListaAsync(string nip, CancellationToken cancellationToken)
    {
        try
        {
            var date = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
            var url = $"https://wl-api.mf.gov.pl/api/search/nip/{nip}?date={date}";

            _log.LogDebug("Biała Lista MF lookup: {Url}", url);

            var response = await _http.GetFromJsonAsync<WlApiResponse>(url, cancellationToken);

            var subject = response?.Result?.Subject;
            if (subject is null)
            {
                _log.LogDebug("Biała Lista: brak podmiotu dla NIP {Nip}", nip);
                return null;
            }

            // Parsuj adres z formatu "ul. Przykładowa 1, 00-001 Warszawa"
            var (street, postalCode, city) = ParseAddress(subject.WorkingAddress ?? subject.ResidenceAddress);

            return new CompanyLookupResult(
                subject.Name ?? string.Empty,
                nip,
                subject.Regon,
                street,
                city,
                postalCode,
                "PL");
        }
        catch (HttpRequestException ex)
        {
            _log.LogWarning("Biała Lista MF HTTP error dla NIP {Nip}: {Message}", nip, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Biała Lista MF lookup failed dla NIP {Nip}", nip);
            return null;
        }
    }

    private static (string? Street, string? PostalCode, string? City) ParseAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return (null, null, null);

        // Format: "ul. Przykładowa 1, 00-001 Warszawa" lub "Przykładowa 1/2, 00-001 Warszawa"
        var commaIdx = address.LastIndexOf(',');
        if (commaIdx < 0) return (address.Trim(), null, null);

        var street = address[..commaIdx].Trim();
        var cityPart = address[(commaIdx + 1)..].Trim();

        // cityPart: "00-001 Warszawa"
        var spaceIdx = cityPart.IndexOf(' ');
        if (spaceIdx > 0 && spaceIdx <= 6)
        {
            var postalCode = cityPart[..spaceIdx].Trim();
            var city = cityPart[(spaceIdx + 1)..].Trim();
            return (street, postalCode, city);
        }

        return (street, null, cityPart);
    }

    // Biała Lista MF response models
    private record WlApiResponse(WlResult? Result);
    private record WlResult(WlSubject? Subject);
    private record WlSubject(
        string? Name,
        string? Nip,
        string? Regon,
        [property: JsonPropertyName("workingAddress")] string? WorkingAddress,
        [property: JsonPropertyName("residenceAddress")] string? ResidenceAddress
    );
}
