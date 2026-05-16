namespace ContractorApp.Application.Common.Interfaces;

public interface INbpService
{
    Task<NbpRateResult?> GetRateAsync(string currencyCode, DateOnly date, CancellationToken cancellationToken = default);
}

public record NbpRateResult(decimal MidRate, DateOnly RateDate, string TableNumber);
