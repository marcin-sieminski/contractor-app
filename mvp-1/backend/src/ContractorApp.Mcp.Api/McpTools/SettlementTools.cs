using System.ComponentModel;
using ContractorApp.Application.Features.Settlements.Commands.FinalizeAnnualSettlement;
using ContractorApp.Application.Features.Settlements.Queries.GetAnnualSettlement;
using ContractorApp.Application.Features.Settlements.Queries.ListAnnualSettlements;
using ContractorApp.Mcp.Api.Services.Ai;
using MediatR;
using ModelContextProtocol.Server;

namespace ContractorApp.Mcp.Api.McpTools;

[McpServerToolType]
public class SettlementTools(ISender mediator)
{
    [McpServerTool(Name = "list_annual_settlements")]
    [Description(
        "Historia rozliczeń rocznych użytkownika (wszystkie lata i formy) ze statusem draft/final " +
        "oraz kwotami dla zatwierdzonych. Używaj do pytań o zeznania roczne PIT-28/36/36L.")]
    public async Task<object> ListAnnualSettlements(CancellationToken ct)
        => await mediator.Send(new ListAnnualSettlementsQuery(), ct);

    [McpServerTool(Name = "get_annual_settlement")]
    [Description(
        "Rozliczenie roczne dla (rok, forma): przeliczenie na żywo dla draftu lub snapshot dla " +
        "zatwierdzonego. Zwraca podatek należny oraz kwotę do zapłaty/nadpłatę. Pamiętaj, że zeznanie " +
        "składa się za rok poprzedni.")]
    public async Task<object> GetAnnualSettlement(
        [Description("Rok rozliczenia, np. 2025.")] int year,
        [Description("Forma opodatkowania: ryczalt | liniowy | skala. Domyślnie liniowy.")] string? taxForm,
        [Description("Etap ZUS dla podpowiedzi składek: ulga_na_start | preferencyjny | pelny. Domyślnie pelny.")] string? zusStage,
        CancellationToken ct)
        => await mediator.Send(new GetAnnualSettlementQuery(year, taxForm ?? "liniowy", zusStage), ct);

    [McpServerTool(Name = "finalize_settlement")]
    [RequiresConfirmation("Zatwierdzenie (zamrożenie) rozliczenia rocznego")]
    [Description("Zatwierdza i zamraża rozliczenie roczne (po zatwierdzeniu edycja wymaga cofnięcia do " +
                 "roboczej). Wymaga potwierdzenia. Rozliczenie musi być wcześniej zapisane i mieć komplet " +
                 "danych podatnika (NIP, imię, nazwisko, kod US).")]
    public async Task<object> FinalizeSettlement(
        [Description("Rok rozliczenia, np. 2025.")] int year,
        [Description("Forma: ryczalt | liniowy | skala. Domyślnie liniowy.")] string? taxForm,
        [Description("Etap ZUS (opcjonalnie).")] string? zusStage,
        CancellationToken ct)
        => await mediator.Send(new FinalizeAnnualSettlementCommand(year, taxForm ?? "liniowy", zusStage), ct);
}
