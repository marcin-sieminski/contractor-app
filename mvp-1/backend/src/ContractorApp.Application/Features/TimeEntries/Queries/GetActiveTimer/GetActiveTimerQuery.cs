using ContractorApp.Application.DTOs;
using MediatR;

namespace ContractorApp.Application.Features.TimeEntries.Queries.GetActiveTimer;

public record GetActiveTimerQuery : IRequest<TimeEntryDto?>;
