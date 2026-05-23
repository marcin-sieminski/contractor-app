using ContractorApp.Application.DTOs;
using MediatR;

namespace ContractorApp.Application.Features.TimeEntries.Commands.ResumeTimer;

public record ResumeTimerCommand(Guid TimeEntryId) : IRequest<TimeEntryDto>;
