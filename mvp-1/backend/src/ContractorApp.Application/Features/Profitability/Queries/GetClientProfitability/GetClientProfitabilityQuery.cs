using MediatR;

namespace ContractorApp.Application.Features.Profitability.Queries.GetClientProfitability;

/// <param name="Year">Rok podatkowy.</param>
/// <param name="Month">Opcjonalnie — filtr do konkretnego miesiąca (1–12). Null = cały rok.</param>
public record GetClientProfitabilityQuery(int Year, int? Month) : IRequest<ProfitabilityDto>;
