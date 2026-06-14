namespace ContractorApp.Application.Features.CashFlow;

public record CashFlowEventDto(
    string Date,
    string Kind,        // "invoice" | "tax"
    string Label,
    decimal Amount,
    string Status);     // "ok" | "tight" | "danger" | "overdue" | "paid"

public record CashFlowDayDto(
    string Date,
    string DayOfWeek,
    decimal Inflows,
    decimal Outflows,
    decimal Balance,
    bool IsObligationDay,
    string? ObligationStatus);  // "ok" | "tight" | "danger" | null

public record CashFlowForecastDto(
    string FromDate,
    string ToDate,
    decimal StartingBalance,
    decimal TotalExpectedInflows,
    decimal TotalObligations,
    decimal TotalAlreadyPaid,
    decimal ProjectedEndBalance,
    decimal MonthlyBufferRecommended,
    decimal AvgMonthlyObligations,
    double AvgDsodays,
    IReadOnlyList<CashFlowDayDto> Days,
    IReadOnlyList<CashFlowEventDto> Events);
