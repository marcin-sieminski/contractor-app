namespace ContractorApp.Application.Features.Settlements;

/// <summary>Pozycja rozliczenia z mapowaniem na formularz (klucz → pole P_xx w XML).</summary>
public sealed record SettlementLineDto(string BoxId, string Label, decimal Value, string? Description);

/// <summary>Wartości wyliczone z danych aplikacji — podpowiedzi do pól edytowalnych.</summary>
public sealed record SettlementPrefillDto(
    decimal Revenue,
    decimal Costs,
    decimal ZusSocialTheoretical,
    decimal ZusHealthTheoretical,
    decimal TaxPrepaymentsTheoretical);

/// <summary>Korekty użytkownika (stan zapisany lub domyślny = wartości teoretyczne).</summary>
public sealed record SettlementAdjustmentsDto(
    decimal? RevenueOverride,
    decimal? CostsOverride,
    decimal ZusSocialPaid,
    decimal ZusHealthPaid,
    decimal TaxPrepaymentsPaid,
    bool IpBoxEnabled,
    decimal IpQualifyingPercent,
    decimal NexusCoefficient);

/// <summary>Dane identyfikacyjne podatnika — wymagane do XML deklaracji i PDF.</summary>
public sealed record TaxpayerDataDto(
    string? Nip,
    string? FirstName,
    string? LastName,
    DateOnly? BirthDate,
    string? Street,
    string? BuildingNumber,
    string? ApartmentNumber,
    string? PostalCode,
    string? City,
    string? TaxOfficeCode,
    string? Voivodeship,
    string? County,
    string? Commune);

/// <summary>Wynik liczbowy rozliczenia (kwoty podstawy/podatku w pełnych złotych).</summary>
public sealed record SettlementResultDto(
    decimal Przychod,
    decimal Koszty,
    decimal Dochod,
    decimal Strata,
    decimal SkladkiSpoleczneOdliczone,
    decimal SkladkaZdrowotnaOdliczona,
    decimal PodstawaOpodatkowania,
    decimal PodstawaIpBox,
    decimal PodatekIpBox,
    decimal PodatekPozaIpBox,
    decimal PodatekNalezny,
    decimal ZaliczkiWplacone,
    decimal DoZaplaty,
    decimal Nadplata,
    decimal EfektywnaStawkaOdPrzychodu);

/// <summary>Pełne rozliczenie roczne dla UI/XML/PDF.</summary>
public sealed record AnnualSettlementDto(
    int Year,
    string TaxForm,
    string TaxFormLabel,
    string FormCode,            // PIT-36 | PIT-36L | PIT-28
    string Status,              // draft | final
    bool IsSaved,
    DateTimeOffset? FinalizedAt,
    bool IsYearParamsExact,
    SettlementPrefillDto Prefill,
    SettlementAdjustmentsDto Adjustments,
    TaxpayerDataDto Taxpayer,
    SettlementResultDto Result,
    IReadOnlyList<SettlementLineDto> Lines,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> DataWarnings);

/// <summary>Pozycja listy rozliczeń (historia).</summary>
public sealed record SettlementListItemDto(
    int Year,
    string TaxForm,
    string TaxFormLabel,
    string FormCode,
    string Status,
    decimal? PodatekNalezny,
    decimal? DoZaplaty,
    decimal? Nadplata,
    DateTimeOffset? FinalizedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Plik wygenerowany z rozliczenia (XML deklaracji lub PDF wydruku).</summary>
public sealed record SettlementFileDto(string FileName, string ContentType, byte[] Content);
