using ContractorApp.Application.Features.Documents;

namespace ContractorApp.Application.Common.Interfaces;

/// <summary>Generuje wydruk PDF zestawienia finansowego (rachunek wyników / bilans).</summary>
public interface IDocumentPdfGenerator
{
    byte[] Generate(FinancialDocumentDto document);
}
