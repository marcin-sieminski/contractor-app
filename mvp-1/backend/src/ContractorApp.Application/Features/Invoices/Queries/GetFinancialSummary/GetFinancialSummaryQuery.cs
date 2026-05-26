using MediatR;

namespace ContractorApp.Application.Features.Invoices.Queries.GetFinancialSummary;

public record GetFinancialSummaryQuery(int? Year = null) : IRequest<FinancialSummaryDto>;

public record FinancialSummaryDto(
    int Year,
    decimal TotalNetPLN,
    decimal TotalGrossPLN,
    decimal TotalExpensesPLN,
    decimal EstimatedIncomeTax,
    decimal EstimatedZusMonthly,
    List<MonthlySummaryDto> ByMonth);

public record MonthlySummaryDto(
    int Month,
    string MonthName,
    decimal NetPLN,
    decimal ExpensesPLN);
