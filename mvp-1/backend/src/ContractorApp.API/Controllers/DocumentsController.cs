using ContractorApp.Application.Features.Documents.Queries.GetFinancialDocument;
using ContractorApp.Application.Features.Documents.Queries.GetFinancialDocumentPdf;
using Microsoft.AspNetCore.Mvc;

namespace ContractorApp.API.Controllers;

/// <summary>
/// Dokumenty i zestawienia finansowe: rachunek wyników oraz bilans uproszczony,
/// generowane za wybrany rok w granulacji miesięcznej, kwartalnej lub rocznej.
/// Podgląd wyliczenia (JSON) oraz wydruk PDF.
/// </summary>
public class DocumentsController : BaseApiController
{
    /// <summary>
    /// Wyliczenie zestawienia do podglądu na widoku.
    /// </summary>
    /// <param name="year">Rok dokumentu.</param>
    /// <param name="type">Rodzaj: income_statement | balance_sheet. Domyślnie income_statement.</param>
    /// <param name="granularity">Granulacja: monthly | quarterly | annual. Domyślnie monthly.</param>
    [HttpGet("{year:int}")]
    public async Task<IActionResult> Get(
        int year, [FromQuery] string? type, [FromQuery] string? granularity, CancellationToken ct)
        => Ok(await Mediator.Send(new GetFinancialDocumentQuery(year, type, granularity), ct));

    /// <summary>Wydruk zestawienia (PDF) z pozycjami w podziale na okresy.</summary>
    [HttpGet("{year:int}/pdf")]
    public async Task<IActionResult> DownloadPdf(
        int year, [FromQuery] string? type, [FromQuery] string? granularity, CancellationToken ct)
    {
        var file = await Mediator.Send(new GetFinancialDocumentPdfQuery(year, type, granularity), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
