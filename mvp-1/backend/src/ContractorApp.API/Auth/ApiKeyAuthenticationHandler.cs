using System.Security.Claims;
using System.Text.Encodings.Web;
using ContractorApp.Infrastructure.Identity;
using ContractorApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContractorApp.API.Auth;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions { }

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ApplicationDbContext db)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? key = null;

        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var value = authHeader.ToString();
            if (value.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
                key = value["ApiKey ".Length..].Trim();
        }

        if (key is null && Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader))
            key = apiKeyHeader.ToString().Trim();

        if (string.IsNullOrEmpty(key))
            return AuthenticateResult.NoResult();

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.McpApiKey == key);

        if (user is null)
            return AuthenticateResult.Fail("Invalid API key.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
