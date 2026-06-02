namespace ContractorApp.Domain.Enums;

/// <summary>Etap składek ZUS w cyklu życia działalności gospodarczej.</summary>
public enum ZusStage
{
    /// <summary>Ulga na start — pierwsze 6 miesięcy, brak składek społecznych.</summary>
    UlgaNaStart = 0,

    /// <summary>Preferencyjny ZUS — miesiące 7–30, obniżone składki społeczne.</summary>
    Preferencyjny = 1,

    /// <summary>Pełny (duży) ZUS — pełne składki społeczne.</summary>
    Pelny = 2
}
