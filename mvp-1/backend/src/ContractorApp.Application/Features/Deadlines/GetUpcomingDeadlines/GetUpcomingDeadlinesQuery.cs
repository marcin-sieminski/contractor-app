using MediatR;

namespace ContractorApp.Application.Features.Deadlines.GetUpcomingDeadlines;

public record GetUpcomingDeadlinesQuery(int DaysAhead = 60) : IRequest<List<DeadlineDto>>;

public record DeadlineDto(
    string Name,
    DateOnly Date,
    string Description,
    int DaysUntil,
    bool IsOverdue);
