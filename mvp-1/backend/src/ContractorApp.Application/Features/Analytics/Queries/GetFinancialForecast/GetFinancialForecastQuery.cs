using MediatR;

namespace ContractorApp.Application.Features.Analytics.Queries.GetFinancialForecast;

/// <summary>
/// Prognoza finansowa na poszczególne miesiące roku metodą run-rate
/// (ekstrapolacja średniej z dotychczasowych miesięcy z danymi).
/// </summary>
/// <param name="Year">Rok prognozy. Domyślnie bieżący.</param>
/// <param name="TaxForm">Forma opodatkowania: ryczalt | liniowy | skala. Domyślnie liniowy.</param>
/// <param name="ZusStage">Etap ZUS: ulga_na_start | preferencyjny | pelny. Domyślnie pelny.</param>
/// <param name="VatRate">Stawka VAT do szacunku VAT naliczonego z kosztów. Domyślnie 0,23.</param>
public record GetFinancialForecastQuery(
    int? Year = null,
    string? TaxForm = null,
    string? ZusStage = null,
    decimal? VatRate = null) : IRequest<FinancialForecastDto>;

public record FinancialForecastDto(
    int Year,
    string TaxForm,
    string ZusStage,
    int MonthsWithData,
    decimal AvgMonthlyRevenue,
    decimal AvgMonthlyCosts,
    ForecastTotalsDto Ytd,
    ForecastTotalsDto FullYear,
    List<MonthForecastDto> Months,
    List<string> Assumptions);

public record MonthForecastDto(
    int Month,
    string MonthName,
    bool IsActual,
    decimal Revenue,
    decimal Costs,
    decimal Income,
    decimal IncomeTax,
    decimal ZusSocial,
    decimal ZusHealth,
    decimal VatOutput,
    decimal VatInput,
    decimal VatPayable,
    decimal TotalObligations,
    decimal NetCashFlow);

public record ForecastTotalsDto(
    decimal Revenue,
    decimal Costs,
    decimal Income,
    decimal IncomeTax,
    decimal ZusSocial,
    decimal ZusHealth,
    decimal VatPayable,
    decimal TotalObligations,
    decimal NetCashFlow);
