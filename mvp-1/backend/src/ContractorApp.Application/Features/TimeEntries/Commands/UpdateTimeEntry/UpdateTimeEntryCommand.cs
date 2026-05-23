using ContractorApp.Application.DTOs;
using MediatR;

namespace ContractorApp.Application.Features.TimeEntries.Commands.UpdateTimeEntry;

public record UpdateTimeEntryCommand(
    Guid Id,
    Guid ProjectId,
    DateTimeOffset StartedAt,
    DateTimeOffset StoppedAt,
    string Description) : IRequest<TimeEntryDto>;
