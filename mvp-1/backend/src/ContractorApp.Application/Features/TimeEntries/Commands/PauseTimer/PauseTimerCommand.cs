using ContractorApp.Application.DTOs;
using MediatR;

namespace ContractorApp.Application.Features.TimeEntries.Commands.PauseTimer;

public record PauseTimerCommand(Guid TimeEntryId) : IRequest<TimeEntryDto>;
