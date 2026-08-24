using ContractorApp.Application.Features.Settlements;
using ContractorApp.Domain.Enums;

namespace ContractorApp.Application.Common.Interfaces;

/// <summary>Gotowy, zwalidowany dokument deklaracji do pobrania/wysyłki.</summary>
public sealed record DeclarationXml(string FileName, string Content, string FormCode, string SchemaVariant);

/// <summary>
/// Budowa XML zeznania rocznego zgodnego ze schemą XSD MF dla jednej formy opodatkowania.
/// Implementacje walidują wynik względem osadzonych schem przed zwróceniem.
/// Przyszła wysyłka przez bramkę e-Deklaracje (SOAP + dane autoryzujące + UPO)
/// konsumuje ten sam dokument — bez zmian w builderach.
/// </summary>
public interface IDeclarationXmlBuilder
{
    TaxForm Form { get; }
    bool Supports(int year);
    DeclarationXml Build(AnnualSettlementDto settlement);
}

public interface IDeclarationXmlBuilderFactory
{
    /// <summary>Builder dla formy i roku; brak wsparcia → DomainException z czytelnym komunikatem.</summary>
    IDeclarationXmlBuilder Resolve(TaxForm form, int year);
}
