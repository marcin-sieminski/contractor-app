using System.Text.Json.Serialization;

namespace ContractorApp.Application.Features.Documents;

/// <summary>Rodzaj kolumny w zestawieniu (steruje wyróżnieniem w UI/PDF). Serializowany jako string.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatementColumnKind
{
    /// <summary>Zwykły okres (miesiąc, kwartał lub stan na dzień).</summary>
    Period,
    /// <summary>Kolumna podsumowania rocznego (suma okresów).</summary>
    Total
}

/// <summary>Styl wiersza zestawienia (steruje pogrubieniem/wcięciem/formatowaniem). Serializowany jako string.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatementRowStyle
{
    /// <summary>Pozycja zwykła.</summary>
    Item,
    /// <summary>Suma częściowa (np. „Koszty działalności", „Aktywa razem").</summary>
    Subtotal,
    /// <summary>Wynik/suma końcowa (wyróżniona).</summary>
    Total,
    /// <summary>Nagłówek sekcji bez wartości (np. „AKTYWA").</summary>
    Section,
    /// <summary>Wiersz informacyjny (np. marża %) — nie sumuje się.</summary>
    Memo
}

/// <summary>Kolumna okresu w matrycy zestawienia.</summary>
public sealed record StatementColumnDto(string Key, string Label, StatementColumnKind Kind);

/// <summary>
/// Wiersz zestawienia. <see cref="Values"/> jest wyrównany do listy kolumn;
/// pusta tablica oznacza wiersz bez wartości (nagłówek sekcji).
/// </summary>
public sealed record StatementRowDto(
    string Key,
    string Label,
    int Indent,
    StatementRowStyle Style,
    IReadOnlyList<decimal?> Values,
    string? Description = null,
    bool IsPercent = false);

/// <summary>Dokument finansowy (rachunek wyników lub bilans) jako matryca pozycje × okresy.</summary>
public sealed record FinancialDocumentDto(
    int Year,
    string Type,            // income_statement | balance_sheet
    string TypeLabel,
    string Granularity,     // monthly | quarterly | annual
    string Title,
    IReadOnlyList<StatementColumnDto> Columns,
    IReadOnlyList<StatementRowDto> Rows,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> DataWarnings,
    bool HasData);

/// <summary>Plik wygenerowany z dokumentu (PDF wydruku).</summary>
public sealed record DocumentFileDto(string FileName, string ContentType, byte[] Content);
