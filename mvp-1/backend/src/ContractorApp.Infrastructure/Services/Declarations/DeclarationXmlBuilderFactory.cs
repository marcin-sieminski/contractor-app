using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Enums;
using ContractorApp.Domain.Exceptions;

namespace ContractorApp.Infrastructure.Services.Declarations;

public class DeclarationXmlBuilderFactory(IEnumerable<IDeclarationXmlBuilder> builders)
    : IDeclarationXmlBuilderFactory
{
    public IDeclarationXmlBuilder Resolve(TaxForm form, int year)
    {
        var builder = builders.FirstOrDefault(b => b.Form == form)
            ?? throw new DomainException($"Brak generatora XML dla formy {form}.");
        if (!builder.Supports(year))
            throw new DomainException(
                $"Generowanie XML {DeclarationSchemaCatalog.FormCode(form)} jest dostępne dla zeznań za rok 2025. "
                + $"Rok {year} nie jest jeszcze obsługiwany (schema MF nieopublikowana lub niedodana).");
        return builder;
    }
}
