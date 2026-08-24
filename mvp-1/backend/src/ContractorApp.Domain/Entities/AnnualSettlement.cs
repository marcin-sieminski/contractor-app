using ContractorApp.Domain.Common;
using ContractorApp.Domain.Enums;

namespace ContractorApp.Domain.Entities;

/// <summary>
/// Roczne rozliczenie podatkowe JDG (PIT-36 / PIT-36L / PIT-28) per użytkownik, rok i forma.
/// W stanie Draft wynik liczony jest na żywo z faktur/wydatków + zapisanych korekt;
/// zatwierdzenie (Final) zamraża pełny wynik w <see cref="CalculationJson"/> —
/// późniejsze zmiany danych źródłowych nie wpływają na zatwierdzone zeznanie.
/// </summary>
public class AnnualSettlement : Entity
{
    public string UserId { get; set; } = string.Empty;
    public int Year { get; set; }
    public TaxForm Form { get; set; }
    public SettlementStatus Status { get; set; } = SettlementStatus.Draft;

    // ── Korekty użytkownika (null = użyj wartości z aplikacji) ──────────────────
    /// <summary>Ręcznie skorygowany roczny przychód (PLN); null = suma z faktur.</summary>
    public decimal? RevenueOverride { get; set; }

    /// <summary>Ręcznie skorygowane roczne koszty (PLN); null = suma z wydatków.</summary>
    public decimal? CostsOverride { get; set; }

    /// <summary>Składki społeczne zapłacone w roku (metoda kasowa).</summary>
    public decimal ZusSocialPaid { get; set; }

    /// <summary>Składka zdrowotna zapłacona w roku (metoda kasowa).</summary>
    public decimal ZusHealthPaid { get; set; }

    /// <summary>Zaliczki na PIT / ryczałt wpłacone w trakcie roku.</summary>
    public decimal TaxPrepaymentsPaid { get; set; }

    // ── IP Box ──────────────────────────────────────────────────────────────────
    public bool IpBoxEnabled { get; set; }
    public decimal IpQualifyingPercent { get; set; }
    public decimal NexusCoefficient { get; set; } = 1m;

    // ── Dane podatnika (wymagane do XML/PDF; aplikacja nie przechowuje ich nigdzie indziej) ──
    public string? TaxpayerNip { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Street { get; set; }
    public string? BuildingNumber { get; set; }
    public string? ApartmentNumber { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }

    // Wymagane w adresie PIT-36/PIT-28 (schema MF); PIT-36L nie zawiera adresu.
    public string? Voivodeship { get; set; }
    public string? County { get; set; }
    public string? Commune { get; set; }

    /// <summary>Czterocyfrowy kod urzędu skarbowego (np. 1471).</summary>
    public string? TaxOfficeCode { get; set; }

    // ── Snapshot zatwierdzonego wyniku ──────────────────────────────────────────
    /// <summary>Pełny zserializowany wynik rozliczenia (JSONB); wypełniany przy zatwierdzeniu.</summary>
    public string? CalculationJson { get; set; }

    public DateTimeOffset? FinalizedAt { get; set; }
}
