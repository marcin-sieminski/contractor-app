namespace ContractorApp.Application.Features.Profitability;

public record ClientProfitabilityDto(
    Guid ClientId,
    string ClientName,
    string ClientNip,
    decimal RevenuePln,
    double BillableHours,
    decimal EffectiveRatePlnPerHour,
    decimal RevenueSharePercent,
    int InvoiceCount,
    IReadOnlyList<ProjectProfitabilityDto> Projects);

public record ProjectProfitabilityDto(
    Guid ProjectId,
    string ProjectName,
    decimal RevenuePln,
    double BillableHours,
    decimal EffectiveRatePlnPerHour);

public record ProfitabilityDto(
    int Year,
    string Period,
    decimal TotalRevenuePln,
    double TotalBillableHours,
    decimal OverallEffectiveRate,
    bool HasConcentrationRisk,
    string? ConcentrationWarning,
    IReadOnlyList<ClientProfitabilityDto> Clients);
