namespace ContractorApp.Domain.Enums;

/// <summary>Status rocznego rozliczenia podatkowego.</summary>
public enum SettlementStatus
{
    /// <summary>Robocze — przeliczane na żywo z faktur i wydatków, edytowalne.</summary>
    Draft = 0,

    /// <summary>Zatwierdzone — wynik zamrożony w snapshocie, tylko do odczytu/eksportu.</summary>
    Final = 1
}
