namespace ContractorApp.Application.Features.CurrencyExposure;

public record CurrencyBreakdownDto(
    string Currency,
    decimal TotalNetForeign,
    decimal TotalNetPln,
    decimal SharePercent,
    int InvoiceCount,
    decimal AvgExchangeRate,
    decimal MinExchangeRate,
    decimal MaxExchangeRate);

public record MonthlyFxDataDto(
    int Month,
    string MonthName,
    decimal PlnRevenue,
    decimal EurRevenuePln,
    decimal UsdRevenuePln,
    decimal GbpRevenuePln,
    decimal ChfRevenuePln,
    decimal TotalRevenuePln,
    decimal? EurRate,
    decimal? UsdRate,
    decimal? GbpRate);

public record SensitivityRowDto(
    int ChangePercent,
    decimal EurImpact,
    decimal UsdImpact,
    decimal GbpImpact,
    decimal ChfImpact,
    decimal TotalImpact,
    decimal AdjustedTotalRevenue);

public record CurrencyExposureDto(
    int Year,
    decimal TotalRevenuePln,
    decimal ForeignRevenuePln,
    decimal PlnOnlyRevenuePln,
    decimal ForeignSharePercent,
    IReadOnlyList<CurrencyBreakdownDto> Breakdown,
    IReadOnlyList<MonthlyFxDataDto> MonthlyTrend,
    IReadOnlyList<SensitivityRowDto> Sensitivity);
