using System.Globalization;
using System.Xml.Linq;
using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.Features.Settlements;
using ContractorApp.Domain.Enums;
using ContractorApp.Domain.Exceptions;

namespace ContractorApp.Infrastructure.Services.Declarations;

/// <summary>
/// Wspólna mechanika builderów deklaracji: nagłówek, dane podatnika, formatowanie kwot,
/// walidacja XSD przed zwróceniem dokumentu. Mapowanie pozycji P_xx — w klasach pochodnych.
/// </summary>
public abstract class PitXmlBuilderBase(DeclarationXsdValidator validator) : IDeclarationXmlBuilder
{
    public abstract TaxForm Form { get; }

    public bool Supports(int year) => DeclarationSchemaCatalog.Find(Form, year) is not null;

    public DeclarationXml Build(AnnualSettlementDto settlement)
    {
        var info = DeclarationSchemaCatalog.Find(Form, settlement.Year)
            ?? throw new DomainException(
                $"Brak schemy {DeclarationSchemaCatalog.FormCode(Form)} dla roku {settlement.Year}. "
                + "Generowanie XML obsługuje obecnie zeznania za rok 2025.");

        ValidateTaxpayer(settlement.Taxpayer);

        var tns = (XNamespace)info.Namespace;
        var deklaracja = new XElement(tns + "Deklaracja",
            BuildNaglowek(tns, info, settlement),
            BuildPodmiot1(tns, info, settlement),
            BuildPozycjeSzczegolowe(tns, settlement));

        var oswiadczenie = BuildOswiadczenie(tns);
        if (oswiadczenie is not null)
            deklaracja.Add(oswiadczenie);

        // Kolejność elementów końcowych różni się między formami
        // (PIT-36: Pouczenia przed Zalaczniki; PIT-36L/PIT-28: odwrotnie).
        var zalaczniki = BuildZalaczniki(tns, settlement);
        var pouczenia = new XElement(tns + "Pouczenia", "1");
        if (PouczeniaBeforeZalaczniki)
        {
            deklaracja.Add(pouczenia);
            if (zalaczniki is not null) deklaracja.Add(zalaczniki);
        }
        else
        {
            if (zalaczniki is not null) deklaracja.Add(zalaczniki);
            deklaracja.Add(pouczenia);
        }

        var document = new XDocument(new XDeclaration("1.0", "UTF-8", null), deklaracja);

        var errors = validator.Validate(document, DeclarationSchemaCatalog.RootSchemaUrl(info));
        if (errors.Count > 0)
            throw new DomainException(
                $"Wygenerowany XML {info.FormCode} nie przechodzi walidacji XSD: "
                + string.Join(" | ", errors.Take(5)));

        var nip = Digits(settlement.Taxpayer.Nip);
        return new DeclarationXml(
            $"{info.FormCode.Replace("/", "")}_{settlement.Year}_{nip}.xml",
            document.Declaration + Environment.NewLine + document,
            info.FormCode,
            info.KodSystemowy);
    }

    /// <summary>Sekcja PozycjeSzczegolowe — mapowanie wyniku rozliczenia na pola P_xx formularza.</summary>
    protected abstract XElement BuildPozycjeSzczegolowe(XNamespace tns, AnnualSettlementDto s);

    /// <summary>Załączniki (PIT/B, PIT/IP) lub null, gdy forma ich nie używa.</summary>
    protected abstract XElement? BuildZalaczniki(XNamespace tns, AnnualSettlementDto s);

    /// <summary>Element Oswiadczenie między pozycjami a załącznikami (wymagany w PIT-36).</summary>
    protected virtual XElement? BuildOswiadczenie(XNamespace tns) => null;

    /// <summary>PIT-36 umieszcza Pouczenia przed sekcją załączników.</summary>
    protected virtual bool PouczeniaBeforeZalaczniki => false;

    /// <summary>
    /// Para „do zapłaty / nadpłata" jest w schemach wyborem (choice) —
    /// emitowany jest wyłącznie element właściwy dla znaku różnicy.
    /// </summary>
    protected static XElement DueOrOverpaid(
        XNamespace tns, string dueName, string overpaidName, decimal due, decimal overpaid) =>
        overpaid > 0m && due <= 0m
            ? new XElement(tns + overpaidName, AmountWhole(overpaid))
            : new XElement(tns + dueName, AmountWhole(due));

    /// <summary>Czy Podmiot1 zawiera adres zamieszkania (PIT-36/PIT-28 tak, PIT-36L nie).</summary>
    protected virtual bool IncludesAddress => true;

    private static XElement BuildNaglowek(XNamespace tns, DeclarationSchemaInfo info, AnnualSettlementDto s) =>
        new(tns + "Naglowek",
            new XElement(tns + "KodFormularza", info.FormCode,
                new XAttribute("kodSystemowy", info.KodSystemowy),
                new XAttribute("kodPodatku", info.KodPodatku),
                new XAttribute("rodzajZobowiazania", "Z"),
                new XAttribute("wersjaSchemy", info.WersjaSchemy)),
            new XElement(tns + "WariantFormularza", info.Variant),
            new XElement(tns + "CelZlozenia", 1, new XAttribute("poz", info.CelZlozeniaPoz)),
            new XElement(tns + "Rok", s.Year),
            new XElement(tns + "KodUrzedu", s.Taxpayer.TaxOfficeCode));

    private XElement BuildPodmiot1(XNamespace tns, DeclarationSchemaInfo info, AnnualSettlementDto s)
    {
        var t = s.Taxpayer;
        var osoba = new XElement(tns + "OsobaFizyczna",
            new XElement(tns + "NIP", Digits(t.Nip)),
            new XElement(tns + "ImiePierwsze", t.FirstName),
            new XElement(tns + "Nazwisko", t.LastName),
            new XElement(tns + "DataUrodzenia",
                t.BirthDate!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                new XAttribute("poz", info.BirthDatePoz)));

        var podmiot = new XElement(tns + "Podmiot1", new XAttribute("rola", "Podatnik"), osoba);

        if (IncludesAddress)
        {
            var adresPol = new XElement(tns + "AdresPol",
                new XElement(tns + "KodKraju", "PL"),
                new XElement(tns + "Wojewodztwo", t.Voivodeship),
                new XElement(tns + "Powiat", t.County),
                new XElement(tns + "Gmina", t.Commune));
            if (!string.IsNullOrWhiteSpace(t.Street))
                adresPol.Add(new XElement(tns + "Ulica", t.Street));
            adresPol.Add(new XElement(tns + "NrDomu", t.BuildingNumber));
            if (!string.IsNullOrWhiteSpace(t.ApartmentNumber))
                adresPol.Add(new XElement(tns + "NrLokalu", t.ApartmentNumber));
            adresPol.Add(
                new XElement(tns + "Miejscowosc", t.City),
                new XElement(tns + "KodPocztowy", t.PostalCode));

            podmiot.Add(new XElement(tns + "AdresZamieszkania",
                new XAttribute("rodzajAdresu", "RAD"), adresPol));
        }

        return podmiot;
    }

    private void ValidateTaxpayer(TaxpayerDataDto t)
    {
        var missing = new List<string>();
        if (!SettlementForms.IsValidNip(t.Nip)) missing.Add("poprawny NIP");
        if (string.IsNullOrWhiteSpace(t.FirstName)) missing.Add("imię");
        if (string.IsNullOrWhiteSpace(t.LastName)) missing.Add("nazwisko");
        if (t.BirthDate is null) missing.Add("data urodzenia");
        if (string.IsNullOrWhiteSpace(t.TaxOfficeCode)) missing.Add("kod urzędu skarbowego");
        if (IncludesAddress)
        {
            if (string.IsNullOrWhiteSpace(t.Voivodeship)) missing.Add("województwo");
            if (string.IsNullOrWhiteSpace(t.County)) missing.Add("powiat");
            if (string.IsNullOrWhiteSpace(t.Commune)) missing.Add("gmina");
            if (string.IsNullOrWhiteSpace(t.BuildingNumber)) missing.Add("nr domu");
            if (string.IsNullOrWhiteSpace(t.City)) missing.Add("miejscowość");
            if (string.IsNullOrWhiteSpace(t.PostalCode)) missing.Add("kod pocztowy");
        }
        if (missing.Count > 0)
            throw new DomainException(
                "Do wygenerowania XML uzupełnij dane podatnika: " + string.Join(", ", missing) + ".");
    }

    // ── Formatowanie wartości wg typów schem MF ────────────────────────────────

    /// <summary>Kwota w złotych i groszach (TKwota2): 1234.56.</summary>
    protected static string Amount2(decimal v) =>
        v.ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>Kwota w pełnych złotych (TKwotaC): 1235.</summary>
    protected static string AmountWhole(decimal v) =>
        decimal.Truncate(v).ToString(CultureInfo.InvariantCulture);

    /// <summary>Udział procentowy (TUdzial): 100.00.</summary>
    protected static string Percent(decimal v) =>
        v.ToString("0.00", CultureInfo.InvariantCulture);

    protected static string Digits(string? value) =>
        new((value ?? string.Empty).Where(char.IsDigit).ToArray());

    /// <summary>Element z kwotą; null gdy warunek niespełniony (pomijany w XML).</summary>
    protected static XElement? AmountIf(XNamespace ns, string name, decimal value, string formatted, bool condition = true) =>
        condition ? new XElement(ns + name, formatted) : null;
}
