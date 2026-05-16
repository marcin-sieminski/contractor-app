using ContractorApp.Application.DTOs;
using MediatR;

namespace ContractorApp.Application.Features.TimeEntries.Commands.StopTimer;

public record StopTimerCommand(Guid TimeEntryId) : IRequest<TimeEntryDto>;
