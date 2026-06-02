namespace ContractorApp.Domain.Enums;

/// <summary>Forma opodatkowania JDG dla polskiego kontraktora B2B.</summary>
public enum TaxForm
{
    /// <summary>Ryczałt od przychodów ewidencjonowanych — 12% dla usług IT.</summary>
    Ryczalt = 0,

    /// <summary>Podatek liniowy 19% od dochodu.</summary>
    Liniowy = 1,

    /// <summary>Skala podatkowa 12% / 32% od dochodu.</summary>
    Skala = 2
}
