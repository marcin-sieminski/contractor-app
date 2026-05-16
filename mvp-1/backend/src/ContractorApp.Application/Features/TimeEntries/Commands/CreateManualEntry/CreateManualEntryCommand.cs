using ContractorApp.Application.DTOs;
using MediatR;

namespace ContractorApp.Application.Features.TimeEntries.Commands.CreateManualEntry;

public record CreateManualEntryCommand(
    Guid ProjectId,
    DateTimeOffset StartedAt,
    DateTimeOffset StoppedAt,
    string Description) : IRequest<TimeEntryDto>;
