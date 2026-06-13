using ContractorApp.Application.Features.Settlements;

namespace ContractorApp.Application.Common.Interfaces;

/// <summary>Wydruk rozliczenia rocznego: pozycje z opisami i mapowaniem na formularz PIT.</summary>
public interface ISettlementPdfGenerator
{
    byte[] Generate(AnnualSettlementDto settlement);
}
