namespace ContractorApp.Application.Features.IpBox;

public record UnfilledIpEntryDto(
    Guid TimeEntryId,
    string ProjectName,
    string StartedAt,
    int? DurationMinutes,
    string Description);

public record MonthIpDataDto(
    int Month,
    string MonthName,
    decimal IpHours,
    decimal TotalHours,
    decimal IpRevenuePln,
    decimal TotalRevenuePln,
    decimal QualifyingRevenuePln);

public record IpBoxProgressDto(
    int Year,
    decimal NexusCoefficient,
    decimal TotalHours,
    decimal IpHours,
    decimal IpHoursPercent,
    decimal TotalRevenuePln,
    decimal IpRevenuePln,
    decimal QualifyingRevenuePln,
    decimal TaxSavingsPln,
    decimal ProjectedYearSavingsPln,
    List<MonthIpDataDto> MonthlyData,
    List<UnfilledIpEntryDto> UnfilledEntries);
