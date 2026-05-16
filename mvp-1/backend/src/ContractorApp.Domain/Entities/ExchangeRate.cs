using ContractorApp.Domain.Common;

namespace ContractorApp.Domain.Entities;

public class ExchangeRate : Entity
{
    public string CurrencyCode { get; set; } = string.Empty;
    public DateOnly RateDate { get; set; }
    public decimal MidRate { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public DateTimeOffset FetchedAt { get; set; } = DateTimeOffset.UtcNow;
}
