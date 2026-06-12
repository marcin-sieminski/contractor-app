using System.Globalization;
using ContractorApp.Application.Features.Analytics.Commands.SaveForecastOverrides;
using ContractorApp.Application.Features.Analytics.Queries.GetFinancialForecast;
using Microsoft.AspNetCore.Mvc;

namespace ContractorApp.API.Controllers;

public class AnalyticsController : BaseApiController
{
    /// <summary>
    /// Prognoza finansowa na poszczególne miesiące roku (przychody, koszty, PIT, ZUS, VAT)
    /// na podstawie zarejestrowanych faktur, kosztów oraz zapisanych ręcznych korekt prognozy.
    /// </summary>
    /// <remarks>
    /// <paramref name="revenueOverrides"/> / <paramref name="costOverrides"/> przyjmują niezapisane korekty
    /// prognozy (podgląd na żywo) w formacie "miesiąc:kwota" rozdzielonym przecinkami, np. "7:15000,8:16000".
    /// Mają pierwszeństwo przed korektami zapisanymi w bazie.
    /// </remarks>
    [HttpGet("forecast")]
    public async Task<IActionResult> GetForecast(
        [FromQuery] int? year,
        [FromQuery] string? taxForm,
        [FromQuery] string? zusStage,
        [FromQuery] decimal? vatRate,
        [FromQuery] bool? includeForecast,
        [FromQuery] bool? ipBoxEnabled,
        [FromQuery] decimal? ipQualifyingPercent,
        [FromQuery] string? revenueOverrides,
        [FromQuery] string? costOverrides,
        CancellationToken ct)
        => Ok(await Mediator.Send(
            new GetFinancialForecastQuery(
                year, taxForm, zusStage, vatRate, includeForecast, ipBoxEnabled, ipQualifyingPercent,
                ParseOverrides(revenueOverrides), ParseOverrides(costOverrides)),
            ct));

    /// <summary>
    /// Zapisuje trwałe ręczne korekty prognozy dla roku (akcja: save | clear | reset).
    /// </summary>
    [HttpPut("forecast/{year:int}/overrides")]
    public async Task<IActionResult> SaveForecastOverrides(
        int year,
        [FromBody] SaveForecastOverridesRequest body,
        CancellationToken ct)
    {
        await Mediator.Send(
            new SaveForecastOverridesCommand(year, body.Action, body.Overrides),
            ct);
        return NoContent();
    }

    public record SaveForecastOverridesRequest(
        string? Action,
        IReadOnlyList<ForecastOverrideItem>? Overrides);

    /// <summary>Parsuje "miesiąc:kwota" rozdzielone przecinkami na słownik; pomija pozycje niepoprawne.</summary>
    private static IReadOnlyDictionary<int, decimal>? ParseOverrides(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var dict = new Dictionary<int, decimal>();
        foreach (var pair in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = pair.Split(':');
            if (parts.Length == 2
                && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var month)
                && month is >= 1 and <= 12
                && decimal.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                && value >= 0)
            {
                dict[month] = value;
            }
        }

        return dict.Count > 0 ? dict : null;
    }
}
