using MediatR;

namespace ContractorApp.Application.Features.Settlements.Commands.SaveAnnualSettlement;

/// <summary>
/// Upsert roboczego rozliczenia (korekty + dane podatnika) i zwrot przeliczonego wyniku.
/// Rozliczenie zatwierdzone (Final) nie podlega edycji — najpierw „Cofnij do roboczej".
/// </summary>
public record SaveAnnualSettlementCommand(
    int Year,
    string TaxForm,
    string? ZusStage,
    decimal? RevenueOverride,
    decimal? CostsOverride,
    decimal ZusSocialPaid,
    decimal ZusHealthPaid,
    decimal TaxPrepaymentsPaid,
    bool IpBoxEnabled,
    decimal IpQualifyingPercent,
    decimal NexusCoefficient,
    TaxpayerDataDto? Taxpayer) : IRequest<AnnualSettlementDto>;
