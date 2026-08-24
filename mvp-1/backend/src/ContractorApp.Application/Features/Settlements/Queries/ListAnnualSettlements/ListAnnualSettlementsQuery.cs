using System.Text.Json;
using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Settlements.Queries.ListAnnualSettlements;

/// <summary>Historia zapisanych rozliczeń użytkownika (wszystkie lata i formy).</summary>
public record ListAnnualSettlementsQuery : IRequest<IReadOnlyList<SettlementListItemDto>>;

public class ListAnnualSettlementsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<ListAnnualSettlementsQuery, IReadOnlyList<SettlementListItemDto>>
{
    public async Task<IReadOnlyList<SettlementListItemDto>> Handle(
        ListAnnualSettlementsQuery request, CancellationToken ct)
    {
        var settlements = await db.AnnualSettlements
            .Where(s => s.UserId == currentUser.UserId)
            .OrderByDescending(s => s.Year).ThenBy(s => s.Form)
            .ToListAsync(ct);

        return settlements.Select(s =>
        {
            // Kwoty tylko dla zatwierdzonych (ze snapshotu) — drafty liczą się na żywo przy otwarciu.
            SettlementResultDto? result = null;
            if (s.Status == SettlementStatus.Final && s.CalculationJson is not null)
                result = JsonSerializer.Deserialize<SettlementSnapshot>(s.CalculationJson)?.Result;

            return new SettlementListItemDto(
                s.Year,
                SettlementForms.Key(s.Form),
                SettlementForms.Label(s.Form),
                SettlementForms.Code(s.Form),
                s.Status == SettlementStatus.Final ? "final" : "draft",
                result?.PodatekNalezny,
                result?.DoZaplaty,
                result?.Nadplata,
                s.FinalizedAt,
                s.UpdatedAt);
        }).ToList();
    }
}
