using MediatR;

namespace ContractorApp.Application.Features.CashFlow.Queries.GetCashFlowForecast;

/// <param name="StartingBalance">Aktualne saldo konta w PLN (punkt startowy prognozy).</param>
/// <param name="TaxForm">liniowy | ryczalt | skala</param>
/// <param name="ZusStage">pelny | preferencyjny | ulga_na_start</param>
public record GetCashFlowForecastQuery(
    decimal StartingBalance,
    string? TaxForm,
    string? ZusStage) : IRequest<CashFlowForecastDto>;
