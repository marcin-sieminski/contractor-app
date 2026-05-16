using System.Net.Http.Json;
using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContractorApp.Infrastructure.Services.Nbp;

public class NbpService : INbpService
{
    private readonly HttpClient _http;
    private readonly ILogger<NbpService> _log;
    // Note: We pass dbContext factory to avoid scoped-in-singleton issues
    private readonly IDbContextFactory<ContractorApp.Infrastructure.Persistence.ApplicationDbContext> _dbFactory;

    public NbpService(
        HttpClient http,
        ILogger<NbpService> log,
        IDbContextFactory<ContractorApp.Infrastructure.Persistence.ApplicationDbContext> dbFactory)
    {
        _http = http;
        _log = log;
        _dbFactory = dbFactory;
    }

    public async Task<NbpRateResult?> GetRateAsync(string currencyCode, DateOnly date, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        // Check cache
        var cached = await db.ExchangeRates
            .FirstOrDefaultAsync(r => r.CurrencyCode == currencyCode && r.RateDate == date, cancellationToken);

        if (cached is not null)
            return new NbpRateResult(cached.MidRate, cached.RateDate, cached.TableNumber);

        // Fetch from NBP API, retrying back for non-business days
        for (int daysBack = 0; daysBack <= 7; daysBack++)
        {
            var tryDate = date.AddDays(-daysBack);
            var url = $"https://api.nbp.pl/api/exchangerates/rates/A/{currencyCode}/{tryDate:yyyy-MM-dd}/?format=json";

            try
            {
                var response = await _http.GetFromJsonAsync<NbpApiResponse>(url, cancellationToken);
                if (response?.Rates is { Length: > 0 })
                {
                    var rate = response.Rates[0];
                    var result = new NbpRateResult(rate.Mid, DateOnly.Parse(rate.EffectiveDate), response.Table);

                    // Persist to cache
                    db.ExchangeRates.Add(new ExchangeRate
                    {
                        CurrencyCode = currencyCode,
                        RateDate = result.RateDate,
                        MidRate = result.MidRate,
                        TableNumber = result.TableNumber
                    });
                    await db.SaveChangesAsync(cancellationToken);

                    return result;
                }
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("404"))
            {
                _log.LogDebug("NBP: no rate for {Currency} on {Date}, trying previous day", currencyCode, tryDate);
            }
        }

        _log.LogWarning("NBP: could not find rate for {Currency} near {Date}", currencyCode, date);
        return null;
    }

    private record NbpApiResponse(string Table, string Currency, string Code, NbpRate[] Rates);
    private record NbpRate(string No, string EffectiveDate, decimal Mid);
}
