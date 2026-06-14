using ContractorApp.Domain.Common;
using ContractorApp.Domain.Enums;

namespace ContractorApp.Domain.Entities;

/// <summary>
/// Rzeczywista wpłata zobowiązania podatkowego/składkowego dokonana przez użytkownika.
/// Pozwala śledzić co już zapłacono (ZUS, PIT, VAT) względem kwot naliczonych przez kalkulator.
/// Wpłaty powiązane są z miesiącem, za który jest zobowiązanie (nie z datą płatności).
/// </summary>
public class TaxPayment : Entity
{
    public string UserId { get; set; } = string.Empty;
    public int Year { get; set; }

    /// <summary>Miesiąc, za który naliczone jest zobowiązanie (1–12).</summary>
    public int Month { get; set; }

    public TaxPaymentType Type { get; set; }

    /// <summary>Kwota faktycznie zapłacona (PLN).</summary>
    public decimal Amount { get; set; }

    /// <summary>Data faktycznej wpłaty (null = zobowiązanie zaplanowane, jeszcze nie zapłacone).</summary>
    public DateOnly? PaidAt { get; set; }

    public string? Notes { get; set; }
}
