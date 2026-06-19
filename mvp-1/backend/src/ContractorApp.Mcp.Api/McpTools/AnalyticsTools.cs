using System.ComponentModel;
using ContractorApp.Application.Features.Analytics.Queries.GetFinancialForecast;
using ContractorApp.Application.Features.CashFlow.Queries.GetCashFlowForecast;
using ContractorApp.Application.Features.CurrencyExposure.Queries.GetCurrencyExposure;
using ContractorApp.Application.Features.Profitability.Queries.GetClientProfitability;
using ContractorApp.Application.Features.TaxObligations.Queries.GetTaxObligations;
using ContractorApp.Application.Features.WorkAnalytics.Queries.GetWorkAnalytics;
using MediatR;
using ModelContextProtocol.Server;

namespace ContractorApp.Mcp.Api.McpTools;

[McpServerToolType]
public class AnalyticsTools(ISender mediator)
{
    [McpServerTool(Name = "get_financial_forecast")]
    [Description(
        "Prognoza finansowa run-rate na cały rok: przychód, koszty, PIT, ZUS (społeczne + zdrowotne), " +
        "VAT i przepływ netto per miesiąc oraz sumy YTD i całoroczne. Opcjonalnie scenariusz IP Box " +
        "z werdyktem i warunkami. Używaj do pytań o prognozę roczną i 'ile mi zostanie'.")]
    public async Task<object> GetFinancialForecast(
        [Description("Rok prognozy. Domyślnie bieżący.")] int? year,
        [Description("Forma opodatkowania: ryczalt | liniowy | skala. Domyślnie liniowy.")] string? taxForm,
        [Description("Etap ZUS: ulga_na_start | preferencyjny | pelny. Domyślnie pelny.")] string? zusStage,
        [Description("Czy policzyć scenariusz ulgi IP Box (tylko liniowy/skala). Domyślnie false.")] bool? ipBoxEnabled,
        [Description("Udział dochodu kwalifikowanego IP w % (0–100, współczynnik Nexus). Domyślnie 100.")] decimal? ipQualifyingPercent,
        CancellationToken ct)
        => await mediator.Send(new GetFinancialForecastQuery(
            Year: year,
            TaxForm: taxForm,
            ZusStage: zusStage,
            IpBoxEnabled: ipBoxEnabled,
            IpQualifyingPercent: ipQualifyingPercent), ct);

    [McpServerTool(Name = "get_cash_flow_forecast")]
    [Description(
        "Prognoza przepływów pieniężnych na 90 dni: spodziewane wpływy z faktur vs zobowiązania " +
        "ZUS/PIT/VAT, rekomendowany miesięczny bufor, projekcja salda końcowego i statusy dni " +
        "(ok/tight/danger/overdue). Używaj do pytań o płynność i 'czy starczy mi na ZUS/podatki'.")]
    public async Task<object> GetCashFlowForecast(
        [Description("Aktualne saldo konta w PLN jako punkt startowy. Domyślnie 0 (prognoza pokazuje wtedy zmianę salda).")] decimal? startingBalance,
        [Description("Forma opodatkowania: ryczalt | liniowy | skala. Domyślnie liniowy.")] string? taxForm,
        [Description("Etap ZUS: ulga_na_start | preferencyjny | pelny. Domyślnie pelny.")] string? zusStage,
        CancellationToken ct)
        => await mediator.Send(new GetCashFlowForecastQuery(startingBalance ?? 0m, taxForm, zusStage), ct);

    [McpServerTool(Name = "get_currency_exposure")]
    [Description(
        "Analiza ekspozycji walutowej za rok: udział przychodu w PLN vs EUR/USD/GBP/CHF, kursy NBP " +
        "(śr/min/max), trend miesięczny oraz analiza wrażliwości (wpływ zmiany kursu o ±%). " +
        "Używaj do pytań o ryzyko walutowe.")]
    public async Task<object> GetCurrencyExposure(
        [Description("Rok. Domyślnie bieżący.")] int? year,
        CancellationToken ct)
        => await mediator.Send(new GetCurrencyExposureQuery(year ?? DateTime.Today.Year), ct);

    [McpServerTool(Name = "get_client_profitability")]
    [Description(
        "Rentowność klientów za rok (opcjonalnie miesiąc): przychód PLN, godziny billable, efektywna " +
        "stawka PLN/h, udział w przychodzie oraz ostrzeżenie o koncentracji (uzależnieniu od jednego " +
        "klienta). Używaj do pytań 'który klient najbardziej się opłaca'.")]
    public async Task<object> GetClientProfitability(
        [Description("Rok. Domyślnie bieżący.")] int? year,
        [Description("Opcjonalny filtr miesiąca 1–12. Null = cały rok.")] int? month,
        CancellationToken ct)
        => await mediator.Send(new GetClientProfitabilityQuery(year ?? DateTime.Today.Year, month), ct);

    [McpServerTool(Name = "get_work_analytics")]
    [Description(
        "Analityka pracy za rok: suma godzin, % zafakturowanych, dni pracujące, średnie godziny/dzień " +
        "i /tydzień, podział miesięczny oraz ALERTY o niezafakturowanej pracy (w tym starszej niż 30 dni). " +
        "Używaj do pytań o produktywność i zaległe fakturowanie.")]
    public async Task<object> GetWorkAnalytics(
        [Description("Rok. Domyślnie bieżący.")] int? year,
        CancellationToken ct)
        => await mediator.Send(new GetWorkAnalyticsQuery(year ?? DateTime.Today.Year), ct);

    [McpServerTool(Name = "get_tax_obligations")]
    [Description(
        "Zobowiązania podatkowe per miesiąc za rok: naliczone PIT/ZUS społeczne/ZUS zdrowotne/VAT vs " +
        "zarejestrowane wpłaty, terminy płatności i status miesiąca (future/paid/partial/overdue/due). " +
        "Używaj do pytań 'ile jestem winien' i 'co zalegam'.")]
    public async Task<object> GetTaxObligations(
        [Description("Rok. Domyślnie bieżący.")] int? year,
        [Description("Forma opodatkowania: ryczalt | liniowy | skala. Domyślnie liniowy.")] string? taxForm,
        [Description("Etap ZUS: ulga_na_start | preferencyjny | pelny. Domyślnie pelny.")] string? zusStage,
        CancellationToken ct)
        => await mediator.Send(new GetTaxObligationsQuery(year ?? DateTime.Today.Year, taxForm, zusStage), ct);
}
