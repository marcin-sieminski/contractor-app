using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractorApp.Infrastructure.Services.KSeF;

public class KsefAuthService
{
    private readonly HttpClient _http;
    private readonly KsefOptions _opts;
    private readonly ILogger<KsefAuthService> _log;

    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public KsefAuthService(HttpClient http, IOptions<KsefOptions> opts, ILogger<KsefAuthService> log)
    {
        _http = http;
        _opts = opts.Value;
        _log = log;
    }

    public async Task<string> GetSessionTokenAsync(string nip, CancellationToken cancellationToken = default)
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiry.AddMinutes(-1))
            return _cachedToken;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiry.AddMinutes(-1))
                return _cachedToken;

            var payload = new
            {
                contextIdentifier = new { type = "onip", identifier = nip },
                credentialsIdentifier = new { type = "onip", identifier = nip },
                credentialsRoleList = new[] { new { type = "user", roleType = "owner", roleName = "owner" } },
                challenge = await GetChallengeAsync(nip, cancellationToken)
            };

            // For token-based auth in test environment
            var tokenPayload = new
            {
                contextIdentifier = new { type = "onip", identifier = nip },
                credentialsToken = _opts.ApiToken
            };

            var response = await _http.PostAsJsonAsync($"{_opts.BaseUrl}/online/Session/initToken", tokenPayload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<KsefTokenResponse>(cancellationToken: cancellationToken);
            _cachedToken = result?.SessionToken ?? throw new InvalidOperationException("KSeF returned empty session token.");
            _tokenExpiry = DateTimeOffset.UtcNow.AddMinutes(9); // KSeF tokens last ~10 min

            _log.LogInformation("KSeF session token obtained, expires {Expiry}", _tokenExpiry);
            return _cachedToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<string> GetChallengeAsync(string nip, CancellationToken cancellationToken)
    {
        var response = await _http.GetFromJsonAsync<KsefChallengeResponse>(
            $"{_opts.BaseUrl}/online/Session/AuthorisationChallenge?contextIdentifier={nip}", cancellationToken);
        return response?.Challenge ?? string.Empty;
    }

    private record KsefTokenResponse(string? SessionToken, string? Timestamp);
    private record KsefChallengeResponse(string? Challenge, string? Timestamp);
}
