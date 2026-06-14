namespace ContractorApp.Application.Features.TaxObligations;

public record TaxObligationsDto(
    int Year,
    string TaxForm,
    string ZusStage,
    TaxObligationsSummaryDto Summary,
    List<MonthObligationDto> Months);

public record TaxObligationsSummaryDto(
    decimal TotalPitDue,
    decimal TotalZusSocialDue,
    decimal TotalZusHealthDue,
    decimal TotalVatDue,
    decimal TotalDue,
    decimal TotalPitPaid,
    decimal TotalZusSocialPaid,
    decimal TotalZusHealthPaid,
    decimal TotalVatPaid,
    decimal TotalPaid,
    decimal TotalRemaining);

public record MonthObligationDto(
    int Month,
    string MonthName,
    bool IsActual,
    // Naliczone zobowiązania
    decimal PitDue,
    decimal ZusSocialDue,
    decimal ZusHealthDue,
    decimal VatDue,
    decimal TotalDue,
    // Terminy płatności
    string ZusDueDate,
    string PitDueDate,
    string VatDueDate,
    // Zarejestrowane wpłaty
    List<TaxPaymentRecordDto> Payments,
    decimal PitPaid,
    decimal ZusSocialPaid,
    decimal ZusHealthPaid,
    decimal VatPaid,
    decimal TotalPaid,
    // Status miesiąca: future | paid | partial | overdue | due
    string Status);

public record TaxPaymentRecordDto(
    Guid Id,
    int Year,
    int Month,
    string Type,
    decimal Amount,
    string? PaidAt,
    string? Notes);
