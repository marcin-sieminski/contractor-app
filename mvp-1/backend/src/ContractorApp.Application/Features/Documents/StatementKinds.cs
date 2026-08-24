namespace ContractorApp.Application.Features.Documents;

/// <summary>Rodzaj dokumentu finansowego generowanego w module Dokumenty.</summary>
public enum StatementType
{
    /// <summary>Rachunek wyników (zysków i strat) — przychody, koszty, wynik za okres.</summary>
    IncomeStatement,
    /// <summary>Bilans uproszczony — stan majątkowy narastająco na koniec okresu.</summary>
    BalanceSheet
}

/// <summary>Granulacja okresów w dokumencie.</summary>
public enum StatementGranularity
{
    /// <summary>Podział na 12 miesięcy (+ kolumna roczna dla rachunku wyników).</summary>
    Monthly,
    /// <summary>Podział na 4 kwartały (+ kolumna roczna dla rachunku wyników).</summary>
    Quarterly,
    /// <summary>Podsumowanie roczne (pojedyncza kolumna).</summary>
    Annual
}

/// <summary>Parsowanie i etykiety rodzajów dokumentów oraz granulacji (klucze stabilne dla API/UI).</summary>
public static class StatementKinds
{
    public static StatementType ParseType(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "balance_sheet" or "bilans" or "balance" => StatementType.BalanceSheet,
        _ => StatementType.IncomeStatement, // income_statement | rachunek_wynikow | pnl
    };

    public static StatementGranularity ParseGranularity(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "annual" or "rocznie" or "yearly" or "year" => StatementGranularity.Annual,
        "quarterly" or "kwartalnie" or "quarter" => StatementGranularity.Quarterly,
        _ => StatementGranularity.Monthly,
    };

    public static string TypeKey(StatementType type) => type switch
    {
        StatementType.BalanceSheet => "balance_sheet",
        _ => "income_statement",
    };

    public static string TypeLabel(StatementType type) => type switch
    {
        StatementType.BalanceSheet => "Bilans (uproszczony)",
        _ => "Rachunek wyników",
    };

    public static string GranularityKey(StatementGranularity gran) => gran switch
    {
        StatementGranularity.Annual => "annual",
        StatementGranularity.Quarterly => "quarterly",
        _ => "monthly",
    };

    /// <summary>Człon nazwy pliku PDF, np. "Rachunek_wynikow".</summary>
    public static string FileStem(StatementType type) => type switch
    {
        StatementType.BalanceSheet => "Bilans",
        _ => "Rachunek_wynikow",
    };
}
