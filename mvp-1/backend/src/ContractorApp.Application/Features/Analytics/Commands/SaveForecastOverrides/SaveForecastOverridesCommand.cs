using MediatR;

namespace ContractorApp.Application.Features.Analytics.Commands.SaveForecastOverrides;

/// <summary>
/// Zapisuje trwałe ręczne korekty prognozy dla danego roku.
/// <list type="bullet">
/// <item><c>save</c> — upsert przekazanych miesięcy (miesiąc bez przychodu i kosztu usuwa korektę);</item>
/// <item><c>clear</c> — wyzerowanie: korekta 0/0 dla wszystkich 12 miesięcy;</item>
/// <item><c>reset</c> — usunięcie wszystkich korekt (powrót do prognozy run-rate / carry-over).</item>
/// </list>
/// </summary>
public record SaveForecastOverridesCommand(
    int Year,
    string? Action,
    IReadOnlyList<ForecastOverrideItem>? Overrides) : IRequest<Unit>;

public record ForecastOverrideItem(int Month, decimal? Revenue, decimal? Cost);
