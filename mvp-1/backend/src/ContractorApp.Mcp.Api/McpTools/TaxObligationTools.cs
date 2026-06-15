using System.ComponentModel;
using ContractorApp.Application.Features.TaxObligations.Commands.RecordTaxPayment;
using MediatR;
using ModelContextProtocol.Server;

namespace ContractorApp.Mcp.Api.McpTools;

[McpServerToolType]
public class TaxObligationTools(ISender mediator)
{
    [McpServerTool(Name = "record_tax_payment")]
    [Description(
        "Rejestruje wpłatę zobowiązania podatkowego/ZUS. Type: zus_social | zus_health | pit | vat. " +
        "Po zapisaniu status danego miesiąca w get_tax_obligations zostanie zaktualizowany.")]
    public async Task<object> RecordTaxPayment(
        [Description("Rok, którego dotyczy zobowiązanie, np. 2026.")] int year,
        [Description("Miesiąc 1–12, którego dotyczy zobowiązanie.")] int month,
        [Description("Typ wpłaty: zus_social | zus_health | pit | vat.")] string type,
        [Description("Kwota wpłaty w PLN.")] decimal amount,
        [Description("Data wpłaty ISO 8601, np. '2026-06-15' (opcjonalnie).")] string? paidAt,
        [Description("Notatka (opcjonalnie).")] string? notes,
        CancellationToken ct)
    {
        var id = await mediator.Send(new RecordTaxPaymentCommand(year, month, type, amount, paidAt, notes), ct);
        return new { recorded = true, paymentId = id, year, month, type, amount };
    }
}
