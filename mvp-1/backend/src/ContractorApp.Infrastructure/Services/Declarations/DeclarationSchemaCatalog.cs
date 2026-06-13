using ContractorApp.Domain.Enums;

namespace ContractorApp.Infrastructure.Services.Declarations;

/// <summary>Metadane schemy jednej deklaracji (forma, rok) — namespace, kody, wariant.</summary>
public sealed record DeclarationSchemaInfo(
    string FormCode,            // PIT-36 | PIT-36L | PIT-28
    int Variant,
    string KodSystemowy,        // np. "PIT36L (21)"
    string KodPodatku,          // PPL / PIT / PPE — atrybut fixed w schemie
    string WersjaSchemy,        // np. "1-0E"
    string CelZlozeniaPoz,      // atrybut poz elementu CelZlozenia (fixed w schemie)
    string BirthDatePoz,        // atrybut poz elementu DataUrodzenia podatnika
    string Namespace,           // tns wzoru, np. http://crd.gov.pl/wzor/2025/09/25/13874/
    int MinYear, int MaxYear);  // lata podatkowe obsługiwane przez wariant

/// <summary>
/// Katalog wariantów schem XSD per (forma, rok podatkowy). Warianty zmieniają się co roku —
/// nowy rok wymaga dodania wpisu i pobrania schem do Resources/Declarations
/// (nazwa pliku = ścieżka URL z '/' zamienionym na '_').
/// </summary>
public static class DeclarationSchemaCatalog
{
    public const string CrdHost = "crd.gov.pl";

    // Przestrzenie wspólne (importy dzielone między formami).
    public const string EtdNamespace = "http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2022/09/13/eD/DefinicjeTypy/";

    // Załączniki PIT/B(22) i PIT/IP(5) — warianty wspólne dla PIT-36 (Z36) i PIT-36L (Z36X).
    public const string PitB36Namespace = "http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2025/08/27/eD/PITB36/";
    public const string PitB36XNamespace = "http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2025/08/27/eD/PITB36X/";
    public const string PitIp36Namespace = "http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2023/10/30/eD/PITIP36/";
    public const string PitIp36XNamespace = "http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2023/11/06/eD/PITIP36X/";

    private static readonly DeclarationSchemaInfo[] Entries =
    [
        // Zeznania za rok 2025 (składane 15.02–30.04.2026). Wartości fixed z TNaglowek schem.
        new("PIT-36", 32, "PIT-36 (32)", "PIT", "1-0E", "P_10", "P_14",
            "http://crd.gov.pl/wzor/2025/09/24/13871/", 2025, 2025),
        new("PIT-36L", 21, "PIT36L (21)", "PPL", "1-0E", "P_6", "P_10",
            "http://crd.gov.pl/wzor/2025/09/25/13874/", 2025, 2025),
        new("PIT-28", 27, "PIT-28 (27)", "PPE", "1-1E", "P_6", "P_10",
            "http://crd.gov.pl/wzor/2025/10/10/13916/", 2025, 2025),
    ];

    public static DeclarationSchemaInfo? Find(TaxForm form, int year)
    {
        var code = FormCode(form);
        return Entries.FirstOrDefault(e => e.FormCode == code && year >= e.MinYear && year <= e.MaxYear);
    }

    public static string FormCode(TaxForm form) => form switch
    {
        TaxForm.Ryczalt => "PIT-28",
        TaxForm.Skala => "PIT-36",
        _ => "PIT-36L"
    };

    /// <summary>Nazwa osadzonego zasobu dla URL schemy (Declarations.&lt;ścieżka z '_'&gt;).</summary>
    public static string ResourceNameForUrl(string url)
    {
        var path = url
            .Replace("https://", "").Replace("http://", "")
            .Replace(CrdHost + "/", "");
        return "Declarations." + path.Replace('/', '_');
    }

    /// <summary>Główna schema wzoru: namespace + "schemat.xsd".</summary>
    public static string RootSchemaUrl(DeclarationSchemaInfo info) => info.Namespace + "schemat.xsd";
}
