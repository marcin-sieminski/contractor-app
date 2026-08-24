using System.ComponentModel;
using ContractorApp.Application.Features.Analytics.Queries.GetFinancialForecast;
using ContractorApp.Application.Features.CashFlow.Queries.GetCashFlowForecast;
using ContractorApp.Application.Features.Deadlines.GetUpcomingDeadlines;
using ContractorApp.Application.Features.Invoices.Queries.GetInvoices;
using ContractorApp.Application.Features.IpBox.Queries.GetIpBoxProgress;
using ContractorApp.Application.Features.TaxObligations.Queries.GetTaxObligations;
using ContractorApp.Application.Features.WorkAnalytics.Queries.GetWorkAnalytics;
using MediatR;
using ModelContextProtocol.Server;

namespace ContractorApp.Mcp.Api.McpTools;

/// <summary>
/// Narzędzia „doradcy" — łączą wiele istniejących zapytań po stronie serwera i zwracają jeden
/// payload gotowy do opisowej odpowiedzi (m.in. lista <see cref="AdvisorSignal"/>). Jedno wywołanie
/// zamiast łańcucha kilku narzędzi (oszczędza iteracje pętli tool-use).
/// </summary>
[McpServerToolType]
public class AdvisorTools(ISender mediator)
{
    [McpServerTool(Name = "get_financial_health_check")]
    [Description(
        "Kompleksowa kontrola kondycji finansowej: łączy prognozę całoroczną, prognozę przepływów " +
        "(płynność i bufor), zobowiązania ZUS/PIT/VAT oraz analitykę pracy. Zwraca podsumowanie i listę " +
        "sygnałów (ryzyka/okazje). Używaj do ogólnych pytań typu 'jak stoję finansowo', 'jaka jest moja " +
        "sytuacja'.")]
    public async Task<object> GetFinancialHealthCheck(
        [Description("Rok. Domyślnie bieżący.")] int? year,
        [Description("Forma opodatkowania: ryczalt | liniowy | skala. Domyślnie liniowy.")] string? taxForm,
        [Description("Etap ZUS: ulga_na_start | preferencyjny | pelny. Domyślnie pelny.")] string? zusStage,
        CancellationToken ct)
    {
        var y = year ?? DateTime.Today.Year;

        var forecast = await mediator.Send(new GetFinancialForecastQuery(Year: y, TaxForm: taxForm, ZusStage: zusStage), ct);
        var cashFlow = await mediator.Send(new GetCashFlowForecastQuery(0m, taxForm, zusStage), ct);
        var obligations = await mediator.Send(new GetTaxObligationsQuery(y, taxForm, zusStage), ct);
        var work = await mediator.Send(new GetWorkAnalyticsQuery(y), ct);

        var signals = new List<AdvisorSignal>();

        if (cashFlow.ProjectedEndBalance < 0)
            signals.Add(new("danger", "Ryzyko utraty płynności",
                $"Prognoza salda na koniec 90 dni jest ujemna ({cashFlow.ProjectedEndBalance:N0} zł przy saldzie startowym 0 zł). Zadbaj o bufor gotówki.",
                cashFlow.ProjectedEndBalance));

        if (cashFlow.MonthlyBufferRecommended > 0)
            signals.Add(new("info", "Rekomendowany bufor miesięczny",
                $"Odkładaj ok. {cashFlow.MonthlyBufferRecommended:N0} zł/mies. na zobowiązania (średnio {cashFlow.AvgMonthlyObligations:N0} zł/mies.).",
                cashFlow.MonthlyBufferRecommended));

        var overdueMonths = obligations.Months.Count(m => m.Status == "overdue");
        if (overdueMonths > 0)
            signals.Add(new("danger", "Zaległe zobowiązania",
                $"{overdueMonths} mies. z zaległymi płatnościami ZUS/PIT/VAT. Łącznie do zapłaty pozostaje {obligations.Summary.TotalRemaining:N0} zł.",
                obligations.Summary.TotalRemaining));
        else if (obligations.Summary.TotalRemaining > 0)
            signals.Add(new("info", "Pozostałe zobowiązania",
                $"Do zapłaty w tym roku pozostaje {obligations.Summary.TotalRemaining:N0} zł.",
                obligations.Summary.TotalRemaining));

        if (work.Summary.UnbilledOlderThan30 > 0)
            signals.Add(new("warning", "Stara niezafakturowana praca",
                $"{work.Summary.UnbilledOlderThan30} wpis(ów) czasu starszych niż 30 dni wciąż niezafakturowanych (łącznie {work.Summary.PendingHours:N1} h oczekuje na fakturę).",
                null));
        else if (work.Summary.PendingHours > 0)
            signals.Add(new("info", "Niezafakturowane godziny",
                $"Masz {work.Summary.PendingHours:N1} h niezafakturowanej pracy (zafakturowano {work.Summary.InvoicedPercent:N0}%).",
                null));

        signals.Add(new(forecast.FullYear.NetCashFlow >= 0 ? "info" : "warning",
            "Prognoza całoroczna (po podatkach i ZUS)",
            $"Przychód: {forecast.FullYear.Revenue:N0} zł, zobowiązania: {forecast.FullYear.TotalObligations:N0} zł, na rękę: {forecast.FullYear.NetCashFlow:N0} zł (forma: {forecast.TaxForm}).",
            forecast.FullYear.NetCashFlow));

        return new
        {
            Year = y,
            TaxForm = forecast.TaxForm,
            ZusStage = forecast.ZusStage,
            Summary = new
            {
                ForecastFullYearRevenue = forecast.FullYear.Revenue,
                ForecastFullYearObligations = forecast.FullYear.TotalObligations,
                ForecastFullYearNetCashFlow = forecast.FullYear.NetCashFlow,
                YtdRevenue = forecast.Ytd.Revenue,
                YtdNetCashFlow = forecast.Ytd.NetCashFlow,
                ProjectedEndBalance90d = cashFlow.ProjectedEndBalance,
                RecommendedMonthlyBuffer = cashFlow.MonthlyBufferRecommended,
                OutstandingObligations = obligations.Summary.TotalRemaining,
                OverdueMonths = overdueMonths,
                PendingInvoicingHours = work.Summary.PendingHours,
                UnbilledOlderThan30 = work.Summary.UnbilledOlderThan30,
                InvoicedPercent = work.Summary.InvoicedPercent,
                MonthsWithData = forecast.MonthsWithData,
            },
            Signals = signals,
            Sources = new[] { "get_financial_forecast", "get_cash_flow_forecast", "get_tax_obligations", "get_work_analytics" },
        };
    }

    [McpServerTool(Name = "get_action_items")]
    [Description(
        "Priorytetyzowana lista 'co wymaga uwagi': zaległe/wymagalne zobowiązania, nadchodzące terminy " +
        "podatkowe, niezafakturowana praca, faktury robocze do finalizacji oraz wpisy bez statusu IP. " +
        "Używaj do pytań 'co mam zrobić', 'co wymaga uwagi', 'na czym się skupić'.")]
    public async Task<object> GetActionItems(
        [Description("Ile dni w przód analizować terminy. Domyślnie 30.")] int? daysAhead,
        CancellationToken ct)
    {
        var y = DateTime.Today.Year;
        var days = daysAhead ?? 30;

        var work = await mediator.Send(new GetWorkAnalyticsQuery(y), ct);
        var deadlines = await mediator.Send(new GetUpcomingDeadlinesQuery(days), ct);
        var obligations = await mediator.Send(new GetTaxObligationsQuery(y, null, null), ct);
        var ipBox = await mediator.Send(new GetIpBoxProgressQuery(y), ct);
        var invoices = await mediator.Send(new GetInvoicesQuery(), ct);

        var items = new List<ActionItem>();

        foreach (var m in obligations.Months.Where(m => m.Status is "overdue" or "partial" or "due"))
        {
            var label = m.Status switch { "overdue" => "zaległe", "partial" => "częściowo opłacone", _ => "do zapłaty" };
            items.Add(new(
                m.Status == "overdue" ? "high" : "medium",
                "Zobowiązania",
                $"{m.MonthName}: {label} ({(m.TotalDue - m.TotalPaid):N0} zł)",
                $"Terminy — ZUS do {m.ZusDueDate}, PIT do {m.PitDueDate}, VAT do {m.VatDueDate}.",
                m.ZusDueDate));
        }

        foreach (var d in deadlines)
            items.Add(new(
                d.IsOverdue ? "high" : d.DaysUntil <= 7 ? "medium" : "low",
                "Termin",
                $"{d.Name} — {(d.IsOverdue ? "po terminie" : $"za {d.DaysUntil} dni")}",
                d.Description,
                d.Date.ToString("yyyy-MM-dd")));

        foreach (var a in work.UnbilledAlerts)
            items.Add(new(
                a.DaysAgo > 30 ? "high" : "medium",
                "Fakturowanie",
                $"Niezafakturowane: {a.ClientName} / {a.ProjectName} ({a.Hours:N1} h)",
                $"Wpis z {a.StartedAt} — {a.DaysAgo} dni temu.",
                null));

        foreach (var inv in invoices.Where(i => i.Status.Equals("Draft", StringComparison.OrdinalIgnoreCase)))
            items.Add(new(
                "medium",
                "Faktury",
                $"Faktura robocza {inv.InvoiceNumber} ({inv.TotalGross:N0} {inv.Currency})",
                $"Klient {inv.ClientName}, wystawiona {inv.IssueDate}. Rozważ finalizację / wysyłkę do KSeF.",
                null));

        if (ipBox.UnfilledEntries.Count > 0)
            items.Add(new(
                "low",
                "IP Box",
                $"Uzupełnij status IP dla {ipBox.UnfilledEntries.Count} wpisów czasu",
                $"Brak oznaczenia pracy IP może zaniżać ulgę. Szac. oszczędność roczna IP Box: {ipBox.ProjectedYearSavingsPln:N0} zł.",
                null));

        var order = new Dictionary<string, int> { ["high"] = 0, ["medium"] = 1, ["low"] = 2 };
        var sorted = items.OrderBy(i => order.GetValueOrDefault(i.Priority, 3)).ToList();

        return new
        {
            Year = y,
            GeneratedAt = DateTime.Today.ToString("yyyy-MM-dd"),
            Count = sorted.Count,
            Items = sorted,
            Sources = new[] { "get_work_analytics", "get_upcoming_deadlines", "get_tax_obligations", "get_ip_box_progress", "get_invoices" },
        };
    }

    [McpServerTool(Name = "get_tax_optimization_advice")]
    [Description(
        "Rekomendacja optymalizacji podatkowej liczona na REALNYCH danych użytkownika (prognoza run-rate): " +
        "porównuje ryczałt/liniowy/skalę pod kątem dochodu netto rocznie oraz liczy scenariusz IP Box " +
        "(oszczędność, werdykt, warunki). Używaj do pytań 'jaka forma się opłaca', 'czy opłaca mi się IP Box', " +
        "'jak zmniejszyć podatki'. Nie pytaj użytkownika o miesięczną kwotę — narzędzie bierze ją z danych.")]
    public async Task<object> GetTaxOptimizationAdvice(
        [Description("Rok. Domyślnie bieżący.")] int? year,
        [Description("Etap ZUS: ulga_na_start | preferencyjny | pelny. Domyślnie pelny.")] string? zusStage,
        [Description("Udział dochodu kwalifikowanego IP w % (0–100, współczynnik Nexus). Domyślnie 100.")] decimal? ipQualifyingPercent,
        CancellationToken ct)
    {
        var y = year ?? DateTime.Today.Year;
        var ipPct = ipQualifyingPercent ?? 100m;

        var liniowy = await mediator.Send(new GetFinancialForecastQuery(Year: y, TaxForm: "liniowy", ZusStage: zusStage), ct);

        if (liniowy.MonthsWithData == 0)
            return new
            {
                Year = y,
                Note = "Brak danych przychodowych w tym roku — dodaj faktury lub wpisy czasu, aby uzyskać rekomendację.",
                Sources = new[] { "get_financial_forecast" },
            };

        var ryczalt = await mediator.Send(new GetFinancialForecastQuery(Year: y, TaxForm: "ryczalt", ZusStage: zusStage), ct);
        var skala = await mediator.Send(new GetFinancialForecastQuery(Year: y, TaxForm: "skala", ZusStage: zusStage), ct);
        var ipBoxForecast = await mediator.Send(new GetFinancialForecastQuery(
            Year: y, TaxForm: "liniowy", ZusStage: zusStage, IpBoxEnabled: true, IpQualifyingPercent: ipPct), ct);

        var forms = new[]
        {
            new { Form = "liniowy", Label = "Liniowy 19%",   AnnualObligations = liniowy.FullYear.TotalObligations, NetCashFlow = liniowy.FullYear.NetCashFlow },
            new { Form = "ryczalt", Label = "Ryczałt 12%",   AnnualObligations = ryczalt.FullYear.TotalObligations, NetCashFlow = ryczalt.FullYear.NetCashFlow },
            new { Form = "skala",   Label = "Skala 12%/32%", AnnualObligations = skala.FullYear.TotalObligations,   NetCashFlow = skala.FullYear.NetCashFlow },
        };
        var best = forms.OrderByDescending(f => f.NetCashFlow).First();

        var signals = new List<AdvisorSignal>
        {
            new("opportunity", $"Optymalna forma: {best.Label}",
                $"Przy prognozie {liniowy.FullYear.Revenue:N0} zł przychodu rocznie najwyższy dochód netto daje forma „{best.Label}\" ({best.NetCashFlow:N0} zł na rękę).",
                best.NetCashFlow),
        };

        var vsLiniowy = best.NetCashFlow - liniowy.FullYear.NetCashFlow;
        if (best.Form != "liniowy" && vsLiniowy > 0)
            signals.Add(new("opportunity", "Potencjalna oszczędność na zmianie formy",
                $"Zmiana z liniowego na „{best.Label}\" to ok. {vsLiniowy:N0} zł rocznie więcej na rękę.",
                vsLiniowy));

        if (ipBoxForecast.IpBox is { } ip)
            signals.Add(new(ip.AnnualSavings > 0 ? "opportunity" : "info",
                $"IP Box: {ip.Verdict}",
                $"Szac. oszczędność IP Box: {ip.AnnualSavings:N0} zł/rok ({ip.MonthlySavings:N0} zł/mies.) przy {ip.QualifyingPercent:N0}% dochodu kwalifikowanego.",
                ip.AnnualSavings));

        return new
        {
            Year = y,
            MonthsWithData = liniowy.MonthsWithData,
            AvgMonthlyRevenue = liniowy.AvgMonthlyRevenue,
            ProjectedAnnualRevenue = liniowy.FullYear.Revenue,
            Comparison = forms,
            Recommended = new { best.Form, best.Label, best.NetCashFlow, best.AnnualObligations },
            IpBox = ipBoxForecast.IpBox,
            Signals = signals,
            Assumptions = liniowy.Assumptions,
            Disclaimer = "Szacunki na bazie prognozy run-rate wg stawek 2026. To nie jest wiążąca porada podatkowa — skonsultuj z księgowym/doradcą.",
            Sources = new[] { "get_financial_forecast (liniowy/ryczalt/skala oraz scenariusz IP Box)" },
        };
    }
}

/// <summary>Sygnał doradczy: severity = danger | warning | info | opportunity.</summary>
public record AdvisorSignal(string Severity, string Title, string Detail, decimal? Amount = null);

/// <summary>Pozycja listy działań. priority = high | medium | low.</summary>
public record ActionItem(string Priority, string Category, string Title, string Detail, string? DueDate);
