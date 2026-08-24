using System.Globalization;
using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.Features.Settlements;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ContractorApp.Infrastructure.Services.Pdf;

/// <summary>
/// Wydruk rozliczenia rocznego (QuestPDF): nagłówek z danymi podatnika, tabela pozycji
/// z polskimi opisami i umiejscowieniem w formularzu, karta wyniku, założenia i zastrzeżenia.
/// </summary>
public class SettlementPdfGenerator : ISettlementPdfGenerator
{
    private static readonly CultureInfo Pl = new("pl-PL");

    public byte[] Generate(AnnualSettlementDto s) =>
        Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(t => t.FontSize(9.5f).FontFamily(Fonts.Verdana));

            page.Header().Element(h => ComposeHeader(h, s));
            page.Content().PaddingVertical(12).Column(col =>
            {
                col.Spacing(12);
                col.Item().Element(e => ComposeResultCard(e, s));
                col.Item().Element(e => ComposeLinesTable(e, s));
                col.Item().Element(e => ComposeNotes(e, s));
            });
            page.Footer().Element(e => ComposeFooter(e, s));
        })).GeneratePdf();

    private static string Money(decimal v) => v.ToString("N2", Pl) + " zł";

    private static void ComposeHeader(IContainer container, AnnualSettlementDto s)
    {
        var t = s.Taxpayer;
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Rozliczenie roczne {s.Year} — {s.FormCode}")
                        .FontSize(16).Bold();
                    c.Item().Text($"Forma opodatkowania: {s.TaxFormLabel}")
                        .FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(150).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text(s.Status == "final" ? "ZATWIERDZONE" : "WERSJA ROBOCZA")
                        .Bold().FontColor(s.Status == "final" ? Colors.Green.Darken2 : Colors.Orange.Darken2);
                    if (s.FinalizedAt is { } f)
                        c.Item().AlignRight().Text(f.ToLocalTime().ToString("d MMMM yyyy", Pl))
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });

            col.Item().PaddingTop(8).Border(0.5f).BorderColor(Colors.Grey.Lighten1)
                .Padding(8).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Podatnik").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        c.Item().Text($"{t.FirstName} {t.LastName}".Trim()).Bold();
                        c.Item().Text($"NIP: {t.Nip ?? "—"}");
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Adres").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        var address = string.Join(" ",
                            new[] { t.Street, t.BuildingNumber }.Where(x => !string.IsNullOrWhiteSpace(x)));
                        if (!string.IsNullOrWhiteSpace(t.ApartmentNumber)) address += $"/{t.ApartmentNumber}";
                        c.Item().Text(string.IsNullOrWhiteSpace(address) ? "—" : address);
                        c.Item().Text($"{t.PostalCode} {t.City}".Trim());
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Urząd skarbowy").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        c.Item().Text($"Kod: {t.TaxOfficeCode ?? "—"}");
                    });
                });
        });
    }

    private static void ComposeResultCard(IContainer container, AnnualSettlementDto s)
    {
        var r = s.Result;
        var isRefund = r.DoZaplaty <= 0m && r.Nadplata > 0m;
        container.Background(isRefund ? Colors.Green.Lighten5 : Colors.Amber.Lighten5)
            .Border(0.5f).BorderColor(isRefund ? Colors.Green.Lighten2 : Colors.Amber.Lighten2)
            .Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(isRefund
                            ? "NADPŁATA (do zwrotu lub zaliczenia na poczet zobowiązań)"
                            : "PODATEK DO ZAPŁATY do urzędu skarbowego")
                        .FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Text(Money(isRefund ? r.Nadplata : r.DoZaplaty)).FontSize(18).Bold();
                });
                row.ConstantItem(220).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text($"Podatek należny: {Money(r.PodatekNalezny)}").FontSize(8.5f);
                    c.Item().AlignRight().Text($"Zaliczki wpłacone: {Money(r.ZaliczkiWplacone)}").FontSize(8.5f);
                    c.Item().AlignRight()
                        .Text($"Efektywna stawka od przychodu: {r.EfektywnaStawkaOdPrzychodu.ToString("N1", Pl)}%")
                        .FontSize(8.5f);
                });
            });
    }

    private static void ComposeLinesTable(IContainer container, AnnualSettlementDto s)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(5);
                columns.ConstantColumn(110);
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten3).Padding(6)
                    .Text($"Pozycja zeznania {s.FormCode}").Bold().FontSize(8.5f);
                header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight()
                    .Text("Kwota").Bold().FontSize(8.5f);
            });

            foreach (var line in s.Lines)
            {
                var highlight = line.BoxId is "PODATEK" or "DO_ZAPLATY" or "NADPLATA";
                var cellBg = highlight ? Colors.Blue.Lighten5 : Colors.White;

                table.Cell().Background(cellBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                    .Padding(6).Column(c =>
                    {
                        c.Item().Text(line.Label).SemiBold();
                        if (!string.IsNullOrWhiteSpace(line.Description))
                            c.Item().Text(line.Description).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    });
                table.Cell().Background(cellBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                    .Padding(6).AlignRight().AlignMiddle()
                    .Text(Money(line.Value)).SemiBold();
            }
        });
    }

    private static void ComposeNotes(IContainer container, AnnualSettlementDto s)
    {
        container.Column(col =>
        {
            col.Spacing(6);
            if (s.DataWarnings.Count > 0)
                col.Item().Background(Colors.Amber.Lighten5).Padding(8).Column(c =>
                {
                    c.Item().Text("Ostrzeżenia dotyczące danych").Bold().FontSize(8.5f);
                    foreach (var w in s.DataWarnings)
                        c.Item().Text("• " + w).FontSize(8);
                });

            col.Item().PaddingTop(4).Text("Założenia i zastrzeżenia").Bold().FontSize(8.5f);
            foreach (var note in s.Notes)
                col.Item().Text("• " + note).FontSize(8).FontColor(Colors.Grey.Darken2);
            col.Item().Text(
                    "• Dokument pomocniczy — nie zastępuje deklaracji podatkowej. Wartości zweryfikuj "
                    + "w eFormularzu MF lub z księgową przed złożeniem zeznania.")
                .FontSize(8).FontColor(Colors.Grey.Darken2);
        });
    }

    private static void ComposeFooter(IContainer container, AnnualSettlementDto s)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(t =>
            {
                t.Span($"Wygenerowano {DateTime.Now.ToString("d MMMM yyyy, HH:mm", Pl)} — ContractorApp")
                    .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
            });
            row.ConstantItem(80).AlignRight().Text(t =>
            {
                t.Span("Strona ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                t.CurrentPageNumber().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                t.Span(" z ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                t.TotalPages().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
            });
        });
    }
}
