using System.ComponentModel;
using ContractorApp.Application.Features.Clients.Queries.GetClients;
using ContractorApp.Application.Features.Invoices.Commands.GenerateInvoice;
using ContractorApp.Application.Features.Invoices.Commands.SubmitToKsef;
using ContractorApp.Application.Features.Invoices.Queries.GetFinancialSummary;
using ContractorApp.Application.Features.Invoices.Queries.GetInvoiceById;
using ContractorApp.Application.Features.Invoices.Queries.GetInvoices;
using ContractorApp.Application.Features.TimeEntries.Queries.GetTimeEntries;
using ContractorApp.Domain.Enums;
using ContractorApp.Infrastructure.Services.KSeF;
using ContractorApp.Mcp.Api.Services.Ai;
using MediatR;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace ContractorApp.Mcp.Api.McpTools;

[McpServerToolType]
public class InvoiceTools(ISender mediator, IOptions<KsefOptions> ksefOptions)
{
    [McpServerTool(Name = "get_invoices")]
    [Description("Returns invoices for the authenticated user. Optionally filter by status (Draft/Submitted/Accepted/Rejected) and/or year.")]
    public async Task<object> GetInvoices(
        [Description("Status filter: Draft, Submitted, Accepted, or Rejected")] string? status,
        [Description("Year filter, e.g. 2025")] int? year,
        CancellationToken ct)
    {
        var all = await mediator.Send(new GetInvoicesQuery(), ct);

        if (status is not null)
            all = all.Where(i => i.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();

        if (year.HasValue)
            all = all.Where(i => i.IssueDate.Year == year.Value).ToList();

        return all;
    }

    [McpServerTool(Name = "get_financial_summary")]
    [Description("Returns a financial summary for the given year: total income (PLN), expenses, estimated PIT (19% linear), and ZUS estimate. Defaults to current year.")]
    public async Task<object> GetFinancialSummary(
        [Description("Year, e.g. 2025. Defaults to current year.")] int? year,
        CancellationToken ct)
        => await mediator.Send(new GetFinancialSummaryQuery(year), ct);

    [McpServerTool(Name = "get_invoice_by_id")]
    [Description("Zwraca szczegóły jednej faktury po identyfikatorze (GUID): pozycje, kwoty, walutę, " +
                 "traktowanie VAT, status i numer KSeF. Identyfikator pobierz z get_invoices.")]
    public async Task<object> GetInvoiceById(
        [Description("Identyfikator faktury (GUID) z get_invoices.")] Guid invoiceId,
        CancellationToken ct)
    {
        var invoice = await mediator.Send(new GetInvoiceByIdQuery(invoiceId), ct);
        return invoice is null
            ? new { error = "Nie znaleziono faktury o podanym identyfikatorze." }
            : invoice;
    }

    [McpServerTool(Name = "generate_invoice")]
    [RequiresConfirmation("Wystawienie nowej faktury z niezafakturowanych wpisów czasu")]
    [Description("Generuje fakturę dla klienta z jego NIEzafakturowanych wpisów czasu w danym miesiącu. " +
                 "Akcja wymaga potwierdzenia użytkownika. VAT: Domestic23 (krajowy 23%) | ReverseCharge " +
                 "(odwrotne obciążenie UE) | OutsideEU | Exempt. Waluta: PLN | EUR | USD | GBP | CHF.")]
    public async Task<object> GenerateInvoice(
        [Description("Nazwa klienta (fragment, bez rozróżniania wielkości liter).")] string clientName,
        [Description("Miesiąc rozliczeniowy w formacie 'YYYY-MM', np. '2026-05'.")] string month,
        [Description("Data wystawienia ISO 8601 (opcjonalnie, domyślnie dziś).")] string? issueDate,
        [Description("Traktowanie VAT: Domestic23 | ReverseCharge | OutsideEU | Exempt. Domyślnie Domestic23.")] string? vatTreatment,
        [Description("Waluta: PLN | EUR | USD | GBP | CHF. Domyślnie PLN.")] string? currency,
        [Description("Termin płatności w dniach. Domyślnie 14.")] int? paymentDays,
        CancellationToken ct)
    {
        var clients = await mediator.Send(new GetClientsQuery(), ct);
        var client = clients.FirstOrDefault(c => c.Name.Contains(clientName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Nie znaleziono klienta '{clientName}'.");

        var parts = month.Split('-');
        var year = int.Parse(parts[0]);
        var mm = int.Parse(parts[1]);
        var from = new DateOnly(year, mm, 1);
        var to = new DateOnly(year, mm, DateTime.DaysInMonth(year, mm));

        var entries = await mediator.Send(new GetTimeEntriesQuery(from, to, null, true), ct);
        var entryIds = entries
            .Where(e => !e.IsInvoiced && e.ClientName.Equals(client.Name, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Id)
            .ToList();

        if (entryIds.Count == 0)
            return new { error = $"Brak niezafakturowanych wpisów czasu dla klienta '{client.Name}' w {month}." };

        var vat = Enum.TryParse<VatTreatment>(vatTreatment, ignoreCase: true, out var v) ? v : VatTreatment.Domestic23;
        var curr = Enum.TryParse<Currency>(currency, ignoreCase: true, out var cu) ? cu : Currency.PLN;
        var issue = issueDate is not null ? DateOnly.Parse(issueDate) : DateOnly.FromDateTime(DateTime.Today);

        return await mediator.Send(new GenerateInvoiceCommand(
            client.Id, entryIds, issue, vat, curr, paymentDays ?? 14), ct);
    }

    [McpServerTool(Name = "submit_invoice_to_ksef")]
    [RequiresConfirmation("Wysłanie faktury do KSeF — operacja nieodwracalna")]
    [Description("Wysyła istniejącą fakturę (po GUID) do Krajowego Systemu e-Faktur (KSeF). Operacja " +
                 "nieodwracalna — wymaga potwierdzenia. GUID pobierz z get_invoices.")]
    public async Task<object> SubmitInvoiceToKsef(
        [Description("GUID faktury z get_invoices.")] Guid invoiceId,
        CancellationToken ct)
        => await mediator.Send(new SubmitToKsefCommand(invoiceId, ksefOptions.Value.TestNip), ct);
}
