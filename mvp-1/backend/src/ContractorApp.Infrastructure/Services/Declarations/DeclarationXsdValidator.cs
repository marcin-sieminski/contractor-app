using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace ContractorApp.Infrastructure.Services.Declarations;

/// <summary>
/// Walidacja dokumentów deklaracji względem osadzonych schem XSD (bez dostępu do sieci).
/// Importy między schemami rozwiązywane są przez mapowanie URL → zasób osadzony.
/// </summary>
public class DeclarationXsdValidator
{
    private static readonly Assembly ResourceAssembly = typeof(DeclarationXsdValidator).Assembly;

    /// <summary>Waliduje dokument względem schemy głównej wzoru; zwraca listę błędów (pusta = OK).</summary>
    public IReadOnlyList<string> Validate(XDocument document, string rootSchemaUrl)
    {
        var schemas = new XmlSchemaSet { XmlResolver = new EmbeddedXsdResolver() };
        using (var rootStream = OpenResource(rootSchemaUrl))
        using (var reader = XmlReader.Create(rootStream,
                   new XmlReaderSettings { XmlResolver = new EmbeddedXsdResolver() }, rootSchemaUrl))
        {
            schemas.Add(null, reader);
        }
        schemas.Compile();

        var errors = new List<string>();
        document.Validate(schemas, (_, e) => errors.Add(e.Message));
        return errors;
    }

    internal static Stream OpenResource(string url)
    {
        var name = DeclarationSchemaCatalog.ResourceNameForUrl(url);
        return ResourceAssembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException(
                $"Brak osadzonej schemy '{name}' dla {url}. Pobierz XSD do Resources/Declarations.");
    }

    /// <summary>Rozwiązuje importy XSD (crd.gov.pl) do zasobów osadzonych — zero sieci w runtime.</summary>
    private sealed class EmbeddedXsdResolver : XmlResolver
    {
        public override object GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
        {
            if (!absoluteUri.Host.EndsWith(DeclarationSchemaCatalog.CrdHost, StringComparison.OrdinalIgnoreCase))
                throw new XmlException($"Odmowa pobrania schemy spoza CRD: {absoluteUri}");
            return OpenResource(absoluteUri.ToString());
        }
    }
}
