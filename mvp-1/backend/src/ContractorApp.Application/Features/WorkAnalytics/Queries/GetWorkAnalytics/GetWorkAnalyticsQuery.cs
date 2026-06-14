using MediatR;

namespace ContractorApp.Application.Features.WorkAnalytics.Queries.GetWorkAnalytics;

public record GetWorkAnalyticsQuery(int Year) : IRequest<WorkAnalyticsDto>;
