using System.Globalization;
using System.Xml.Linq;
using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Entities;
using ContractorApp.Domain.Enums;

namespace ContractorApp.Infrastructure.Services.KSeF;

/// <summary>
/// Generates KSeF FA(3) compliant XML invoice (schema FA_VAT v11-0E).
/// Handles: PLN domestic (23% VAT), reverse charge (EU), outside EU (np.).
/// </summary>
public class KsefXmlBuilder : IKsefXmlBuilder
{
    private static readonly XNamespace Fa = "http://crd.gov.pl/wzor/2023/06/29/12648/";
    private static readonly XNamespace Etd = "http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2022/01/05/eD/DefinicjeTypy/";

    public string Build(Invoice invoice, string sellerNip)
    {
        var now = DateTimeOffset.UtcNow;
        var isReverseCharge = invoice.VatTreatment == VatTreatment.ReverseCharge;
        var isOutsideEu = invoice.VatTreatment == VatTreatment.OutsideEU;
        var isExempt = invoice.VatTreatment == VatTreatment.Exempt;
        var isDomestic = invoice.VatTreatment == VatTreatment.Domestic23;

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(Fa + "Faktura",
                new XAttribute(XNamespace.Xmlns + "fa", Fa.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "etd", Etd.NamespaceName),

                // Header
                new XElement(Fa + "Naglowek",
                    new XElement(Fa + "KodFormularza",
                        new XAttribute("kodSystemowy", "FA (3)"),
                        new XAttribute("wersjaSchemy", "1-0E"),
                        "FA"),
                    new XElement(Fa + "WariantFormularza", "3"),
                    new XElement(Fa + "DataWytworzeniaFa", now.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                    new XElement(Fa + "SystemInfo", "ContractorApp MVP")
                ),

                // Seller (Podmiot1)
                BuildPodmiot1(sellerNip),

                // Buyer (Podmiot2)
                BuildPodmiot2(invoice.Client),

                // Invoice body
                new XElement(Fa + "Fa",
                    new XElement(Fa + "KodWaluty", invoice.Currency.ToString()),
                    new XElement(Fa + "P_1", invoice.IssueDate.ToString("yyyy-MM-dd")),
                    new XElement(Fa + "P_1M", invoice.IssueDate.Month.ToString()),
                    new XElement(Fa + "P_1R", invoice.IssueDate.Year.ToString()),
                    new XElement(Fa + "P_2", invoice.InvoiceNumber),
                    new XElement(Fa + "P_6", invoice.ServiceDate.ToString("yyyy-MM-dd")),

                    // Line items
                    invoice.LineItems.OrderBy(l => l.LineNumber).Select(l => BuildLineItem(l, invoice.VatTreatment)),

                    // Totals
                    BuildTotals(invoice),

                    // Annotations
                    new XElement(Fa + "Adnotacje",
                        new XElement(Fa + "P_16", isDomestic ? "2" : "1"),  // self-billing: 2=no
                        new XElement(Fa + "P_17", "2"),                      // cash accounting: 2=no
                        new XElement(Fa + "P_18", isReverseCharge ? "1" : "2"), // reverse charge
                        new XElement(Fa + "P_18A", "2"),  // art. 17 para 1 pt 7/8: no
                        new XElement(Fa + "P_19", "2"),   // tourist service: no
                        new XElement(Fa + "P_22", "2"),   // new transport: no
                        new XElement(Fa + "P_23", "2"),   // simplified: no
                        new XElement(Fa + "P_PMarzy", "2") // margin: no
                    ),

                    // Exchange rate info for foreign currency
                    invoice.Currency != Currency.PLN && invoice.ExchangeRate.HasValue
                        ? new XElement(Fa + "KursWaluty",
                            new XElement(Fa + "KodWalutyOryginalnej", invoice.Currency.ToString()),
                            new XElement(Fa + "KursWalutyZ", "1"),
                            new XElement(Fa + "NazwaTabeli", invoice.ExchangeRateTableNumber ?? ""),
                            new XElement(Fa + "KursWalutyNa", FormatDecimal(invoice.ExchangeRate.Value)),
                            new XElement(Fa + "DataKursuWaluty", invoice.ExchangeRateDate?.ToString("yyyy-MM-dd") ?? ""))
                        : null!
                )
            )
        );

        return doc.ToString(SaveOptions.None);
    }

    private XElement BuildPodmiot1(string sellerNip) =>
        new(Fa + "Podmiot1",
            new XElement(Fa + "DaneIdentyfikacyjne",
                new XElement(Fa + "NIP", sellerNip),
                new XElement(Fa + "PelnaNazwa", "Freelancer")  // Seller name from config in production
            ),
            new XElement(Fa + "Adres",
                new XElement(Fa + "KodKraju", "PL"),
                new XElement(Fa + "AdresL1", "ul. Przykładowa 1"),
                new XElement(Fa + "AdresL2", "00-001 Warszawa")
            ),
            new XElement(Fa + "Rola", "1")
        );

    private XElement BuildPodmiot2(Client client) =>
        new(Fa + "Podmiot2",
            new XElement(Fa + "DaneIdentyfikacyjne",
                string.IsNullOrEmpty(client.Country) || client.Country == "PL"
                    ? new XElement(Fa + "NIP", client.Nip)
                    : (object)new XElement(Fa + "NrId", client.Nip),
                new XElement(Fa + "PelnaNazwa", client.Name)
            ),
            new XElement(Fa + "Adres",
                new XElement(Fa + "KodKraju", client.Country ?? "PL"),
                new XElement(Fa + "AdresL1", client.Street ?? ""),
                new XElement(Fa + "AdresL2", $"{client.PostalCode} {client.City}".Trim())
            ),
            new XElement(Fa + "Rola", "2")
        );

    private XElement BuildLineItem(InvoiceLineItem line, VatTreatment vatTreatment)
    {
        var vatCode = vatTreatment switch
        {
            VatTreatment.Domestic23 => "23",
            VatTreatment.ReverseCharge => "OO",  // odwrotne obciążenie
            VatTreatment.OutsideEU => "np.",
            VatTreatment.Exempt => "ZW",
            _ => "23"
        };

        return new XElement(Fa + "FaWiersz",
            new XElement(Fa + "NrWierszaFa", line.LineNumber.ToString()),
            new XElement(Fa + "P_7", line.Description),
            new XElement(Fa + "P_8A", line.Unit),
            new XElement(Fa + "P_8B", FormatDecimal(line.Quantity)),
            new XElement(Fa + "P_9A", FormatDecimal(line.UnitPrice)),
            new XElement(Fa + "P_11", FormatDecimal(line.NetAmount)),
            new XElement(Fa + "P_12", vatCode)
        );
    }

    private XElement BuildTotals(Invoice invoice)
    {
        var elements = new List<XElement>();

        if (invoice.VatTreatment == VatTreatment.Domestic23)
        {
            elements.Add(new XElement(Fa + "P_13_1", FormatDecimal(invoice.TotalNet)));
            elements.Add(new XElement(Fa + "P_14_1", FormatDecimal(invoice.TotalVat)));
        }
        else
        {
            // Reverse charge or outside EU — no VAT, show in exempt/np. bucket
            elements.Add(new XElement(Fa + "P_13_8", FormatDecimal(invoice.TotalNet)));
        }

        elements.Add(new XElement(Fa + "P_15", FormatDecimal(invoice.TotalGross)));

        return new XElement(Fa + "Sumy", elements);
    }

    private static string FormatDecimal(decimal value) =>
        value.ToString("F2", CultureInfo.InvariantCulture);
}
