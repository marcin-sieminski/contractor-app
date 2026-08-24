namespace ContractorApp.Application.DTOs;

public record TimeEntryDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string ClientName,
    DateTimeOffset StartedAt,
    DateTimeOffset? StoppedAt,
    int? DurationMinutes,
    string Description,
    bool IsInvoiced,
    bool IsRunning,
    bool IsPaused,
    int AccumulatedSeconds);
