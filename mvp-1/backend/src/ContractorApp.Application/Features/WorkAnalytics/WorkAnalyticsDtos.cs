namespace ContractorApp.Application.Features.WorkAnalytics;

public record WorkAnalyticsSummaryDto(
    double TotalHours,
    double InvoicedHours,
    double PendingHours,
    decimal InvoicedPercent,
    int WorkedDays,
    int BusinessDaysInPeriod,
    int FreeDays,
    double AvgHoursPerWorkedDay,
    double AvgHoursPerWeek,
    int UnbilledOlderThan30);

public record MonthWorkDto(
    int Month,
    string MonthName,
    double TotalHours,
    double InvoicedHours,
    double PendingHours,
    decimal InvoicedPercent,
    int WorkedDays);

public record DayWorkDto(
    string Date,
    double Hours,
    bool HasInvoiced);

public record UnbilledAlertDto(
    Guid EntryId,
    string ProjectName,
    string ClientName,
    double Hours,
    string StartedAt,
    int DaysAgo);

public record WorkAnalyticsDto(
    int Year,
    WorkAnalyticsSummaryDto Summary,
    IReadOnlyList<MonthWorkDto> Months,
    IReadOnlyList<DayWorkDto> Days,
    IReadOnlyList<UnbilledAlertDto> UnbilledAlerts);
