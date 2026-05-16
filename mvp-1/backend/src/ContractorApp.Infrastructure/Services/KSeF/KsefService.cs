using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using ContractorApp.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractorApp.Infrastructure.Services.KSeF;

public class KsefOptions
{
    public string BaseUrl { get; set; } = "https://ksef-test.mf.gov.pl/api";
    public string TestNip { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
    public bool UseFake { get; set; } = false;
}

public class KsefService : IKsefService
{
    private readonly HttpClient _http;
    private readonly KsefAuthService _auth;
    private readonly KsefOptions _opts;
    private readonly ILogger<KsefService> _log;

    public KsefService(HttpClient http, KsefAuthService auth, IOptions<KsefOptions> opts, ILogger<KsefService> log)
    {
        _http = http;
        _auth = auth;
        _opts = opts.Value;
        _log = log;
    }

    public async Task<KsefSubmitResult> SubmitInvoiceAsync(string xmlContent, string sellerNip, CancellationToken cancellationToken = default)
    {
        if (_opts.UseFake)
        {
            _log.LogInformation("KSeF FAKE mode — returning fake reference number");
            return new KsefSubmitResult(true, $"FAKE-{Guid.NewGuid():N}", null);
        }

        try
        {
            var token = await _auth.GetSessionTokenAsync(sellerNip, cancellationToken);
            var xmlBytes = Encoding.UTF8.GetBytes(xmlContent);
            var hash = Convert.ToBase64String(SHA256.HashData(xmlBytes));
            var xmlBase64 = Convert.ToBase64String(xmlBytes);

            var payload = new
            {
                invoiceHash = new { hashSHA = new { algorithm = "SHA-256", encoding = "Base64", value = hash }, fileSize = xmlBytes.Length },
                invoicePayload = new { type = "plain", invoiceBody = xmlBase64 }
            };

            _http.DefaultRequestHeaders.Remove("SessionToken");
            _http.DefaultRequestHeaders.Add("SessionToken", token);

            var response = await _http.PutAsJsonAsync($"{_opts.BaseUrl}/online/Invoice/send", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken);
                _log.LogError("KSeF submission failed: {Status} {Body}", response.StatusCode, err);
                return new KsefSubmitResult(false, null, $"HTTP {(int)response.StatusCode}: {err}");
            }

            var result = await response.Content.ReadFromJsonAsync<KsefSendResponse>(cancellationToken: cancellationToken);
            return new KsefSubmitResult(true, result?.ElementReferenceNumber, null);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "KSeF submission exception");
            return new KsefSubmitResult(false, null, ex.Message);
        }
    }

    private record KsefSendResponse(string? ElementReferenceNumber, string? Timestamp, string? ReferenceNumber);
}
