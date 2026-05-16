using ContractorApp.Application.DTOs;
using MediatR;

namespace ContractorApp.Application.Features.TimeEntries.Commands.StartTimer;

public record StartTimerCommand(Guid ProjectId, string Description) : IRequest<TimeEntryDto>;
