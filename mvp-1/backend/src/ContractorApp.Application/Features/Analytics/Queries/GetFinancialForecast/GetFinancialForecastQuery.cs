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
/// <param name="IncludeForecast">Czy dołączać prognozę run-rate dla miesięcy bez danych. Domyślnie true.</param>
/// <param name="IpBoxEnabled">Czy policzyć scenariusz ulgi IP Box (tylko liniowy/skala). Domyślnie false.</param>
/// <param name="IpQualifyingPercent">Udział dochodu kwalifikowanego jako IP (0–100, współczynnik Nexus). Domyślnie 100.</param>
/// <param name="RevenueOverrides">Niezapisane korekty prognozowanego przychodu (miesiąc 1–12 → kwota netto PLN)
/// do podglądu na żywo. Pierwszeństwo przed korektami zapisanymi w bazie. Tylko miesiące prognozy.</param>
/// <param name="CostOverrides">Niezapisane korekty prognozowanych kosztów (miesiąc 1–12 → kwota brutto PLN), jak wyżej.</param>
public record GetFinancialForecastQuery(
    int? Year = null,
    string? TaxForm = null,
    string? ZusStage = null,
    decimal? VatRate = null,
    bool? IncludeForecast = null,
    bool? IpBoxEnabled = null,
    decimal? IpQualifyingPercent = null,
    IReadOnlyDictionary<int, decimal>? RevenueOverrides = null,
    IReadOnlyDictionary<int, decimal>? CostOverrides = null) : IRequest<FinancialForecastDto>;

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
    List<string> Assumptions,
    IpBoxScenarioDto? IpBox = null);

/// <summary>
/// Roczne porównanie obciążeń przy zastosowaniu ulgi IP Box (5% PIT na dochód kwalifikowany)
/// względem formy bazowej. ZUS, składka zdrowotna i VAT pozostają bez zmian.
/// </summary>
public record IpBoxScenarioDto(
    decimal QualifyingPercent,
    decimal NexusCoefficient,
    decimal AnnualPitWithout,
    decimal AnnualPitWith,
    decimal AnnualSavings,
    decimal MonthlySavings,
    decimal TotalObligationsWithout,
    decimal TotalObligationsWith,
    decimal NetCashFlowWith,
    decimal EffectiveRateWithout,
    decimal EffectiveRateWith,
    string Verdict,
    List<string> Conditions);

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
    decimal NetCashFlow,
    bool IsEdited = false);

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
