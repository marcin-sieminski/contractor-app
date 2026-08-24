using ContractorApp.Application.Features.Settlements;
using ContractorApp.Application.Features.Settlements.Commands.FinalizeAnnualSettlement;
using ContractorApp.Application.Features.Settlements.Commands.ReopenAnnualSettlement;
using ContractorApp.Application.Features.Settlements.Commands.SaveAnnualSettlement;
using ContractorApp.Application.Features.Settlements.Queries.GetAnnualSettlement;
using ContractorApp.Application.Features.Settlements.Queries.GetSettlementPdf;
using ContractorApp.Application.Features.Settlements.Queries.GetSettlementXml;
using ContractorApp.Application.Features.Settlements.Queries.ListAnnualSettlements;
using Microsoft.AspNetCore.Mvc;

namespace ContractorApp.API.Controllers;

/// <summary>
/// Rozliczenie roczne PIT (PIT-36 / PIT-36L / PIT-28): wyliczenie wartości do zeznania,
/// zapis per rok i forma, zatwierdzanie oraz eksport XML/PDF.
/// </summary>
public class SettlementsController : BaseApiController
{
    /// <summary>Historia zapisanych rozliczeń (wszystkie lata i formy).</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await Mediator.Send(new ListAnnualSettlementsQuery(), ct));

    /// <summary>
    /// Rozliczenie za rok dla formy opodatkowania (ryczalt | liniowy | skala).
    /// Niezapisane → wirtualny draft z podpowiedziami; zatwierdzone → wynik ze snapshotu.
    /// </summary>
    [HttpGet("{year:int}/{taxForm}")]
    public async Task<IActionResult> Get(
        int year, string taxForm, [FromQuery] string? zusStage, CancellationToken ct)
        => Ok(await Mediator.Send(new GetAnnualSettlementQuery(year, taxForm, zusStage), ct));

    /// <summary>Zapisuje korekty i dane podatnika (upsert wersji roboczej).</summary>
    [HttpPut("{year:int}/{taxForm}")]
    public async Task<IActionResult> Save(
        int year, string taxForm, [FromQuery] string? zusStage,
        [FromBody] SaveSettlementRequest body, CancellationToken ct)
        => Ok(await Mediator.Send(new SaveAnnualSettlementCommand(
            year, taxForm, zusStage,
            body.RevenueOverride, body.CostsOverride,
            body.ZusSocialPaid, body.ZusHealthPaid, body.TaxPrepaymentsPaid,
            body.IpBoxEnabled, body.IpQualifyingPercent, body.NexusCoefficient,
            body.Taxpayer), ct));

    /// <summary>Zatwierdza rozliczenie (zamraża wynik; wymaga kompletnych danych podatnika).</summary>
    [HttpPost("{year:int}/{taxForm}/finalize")]
    public async Task<IActionResult> Finalize(
        int year, string taxForm, [FromQuery] string? zusStage, CancellationToken ct)
        => Ok(await Mediator.Send(new FinalizeAnnualSettlementCommand(year, taxForm, zusStage), ct));

    /// <summary>
    /// XML zeznania zgodny ze schemą XSD MF — do wgrania w eFormularzu
    /// (klient-eformularz.mf.gov.pl) i autoryzacji wysyłki po stronie MF.
    /// </summary>
    [HttpGet("{year:int}/{taxForm}/xml")]
    public async Task<IActionResult> DownloadXml(
        int year, string taxForm, [FromQuery] string? zusStage, CancellationToken ct)
    {
        var file = await Mediator.Send(new GetSettlementXmlQuery(year, taxForm, zusStage), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Wydruk rozliczenia (PDF) z opisami pozycji i mapowaniem na pola formularza.</summary>
    [HttpGet("{year:int}/{taxForm}/pdf")]
    public async Task<IActionResult> DownloadPdf(
        int year, string taxForm, [FromQuery] string? zusStage, CancellationToken ct)
    {
        var file = await Mediator.Send(new GetSettlementPdfQuery(year, taxForm, zusStage), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Cofa zatwierdzone rozliczenie do wersji roboczej.</summary>
    [HttpPost("{year:int}/{taxForm}/reopen")]
    public async Task<IActionResult> Reopen(
        int year, string taxForm, [FromQuery] string? zusStage, CancellationToken ct)
        => Ok(await Mediator.Send(new ReopenAnnualSettlementCommand(year, taxForm, zusStage), ct));

    public record SaveSettlementRequest(
        decimal? RevenueOverride,
        decimal? CostsOverride,
        decimal ZusSocialPaid,
        decimal ZusHealthPaid,
        decimal TaxPrepaymentsPaid,
        bool IpBoxEnabled,
        decimal IpQualifyingPercent,
        decimal NexusCoefficient,
        TaxpayerDataDto? Taxpayer);
}
