using ContractorApp.Application.DTOs;
using MediatR;

namespace ContractorApp.Application.Features.TimeEntries.Queries.GetTimeEntries;

public record GetTimeEntriesQuery(
    DateOnly? From,
    DateOnly? To,
    Guid? ProjectId,
    bool IncludeInvoiced = true) : IRequest<List<TimeEntryDto>>;
