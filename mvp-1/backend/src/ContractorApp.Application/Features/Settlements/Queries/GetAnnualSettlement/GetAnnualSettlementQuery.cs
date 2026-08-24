using MediatR;

namespace ContractorApp.Application.Features.Settlements.Queries.GetAnnualSettlement;

/// <summary>
/// Rozliczenie roczne dla (rok, forma). Brak zapisanego rozliczenia → „wirtualny draft"
/// z wartościami teoretycznymi; Draft → przeliczenie na żywo; Final → snapshot.
/// <paramref name="ZusStage"/> wpływa wyłącznie na teoretyczne podpowiedzi składek.
/// </summary>
public record GetAnnualSettlementQuery(int Year, string TaxForm, string? ZusStage)
    : IRequest<AnnualSettlementDto>;
