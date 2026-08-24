using System.Globalization;
using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.Features.Documents;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ContractorApp.Infrastructure.Services.Pdf;

/// <summary>
/// Wydruk zestawienia finansowego (QuestPDF) jako matryca pozycje × okresy.
/// Rachunek wyników / bilans uproszczony; ujęcie miesięczne renderowane poziomo (landscape),
/// kwartalne i roczne — pionowo. Styl spójny z wydrukiem rozliczenia rocznego.
/// </summary>
public class DocumentPdfGenerator : IDocumentPdfGenerator
{
    private static readonly CultureInfo Pl = new("pl-PL");

    public byte[] Generate(FinancialDocumentDto doc)
    {
        var isMonthly = doc.Granularity == "monthly";
        var decimals = isMonthly ? 0 : 2; // miesięcznie bez groszy — by zmieścić 13 kolumn

        return Document.Create(container => container.Page(page =>
        {
            page.Size(isMonthly ? PageSizes.A4.Landscape() : PageSizes.A4);
            page.Margin(34);
            page.DefaultTextStyle(t => t.FontSize(isMonthly ? 8f : 9.5f).FontFamily(Fonts.Verdana));

            page.Header().Element(h => ComposeHeader(h, doc));
            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Spacing(10);
                if (doc.DataWarnings.Count > 0)
                    col.Item().Element(e => ComposeWarnings(e, doc));
                if (!doc.HasData)
                    col.Item().Text("Brak zarejestrowanych faktur i wydatków w wybranym roku — wartości zerowe.")
                        .FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                col.Item().Element(e => ComposeTable(e, doc, decimals));
                col.Item().Element(e => ComposeNotes(e, doc));
            });
            page.Footer().Element(ComposeFooter);
        })).GeneratePdf();
    }

    private static string GranularityLabel(string g) => g switch
    {
        "annual" => "ujęcie roczne",
        "quarterly" => "ujęcie kwartalne",
        _ => "ujęcie miesięczne",
    };

    private static string FormatValue(decimal? v, bool isPercent, int decimals)
    {
        if (v is null) return "—";
        return isPercent
            ? v.Value.ToString("N1", Pl) + "%"
            : v.Value.ToString("N" + decimals, Pl);
    }

    private static void ComposeHeader(IContainer container, FinancialDocumentDto doc)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(doc.Title).FontSize(15).Bold();
                    c.Item().Text($"{doc.TypeLabel} · {GranularityLabel(doc.Granularity)} · wartości w PLN")
                        .FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(150).AlignRight().Text($"Rok {doc.Year}")
                    .FontSize(11).SemiBold().FontColor(Colors.Grey.Darken2);
            });
            col.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
        });
    }

    private static void ComposeWarnings(IContainer container, FinancialDocumentDto doc)
    {
        container.Background(Colors.Amber.Lighten5).Border(0.5f).BorderColor(Colors.Amber.Lighten2)
            .Padding(8).Column(c =>
            {
                c.Item().Text("Ostrzeżenia dotyczące danych").Bold().FontSize(8.5f);
                foreach (var w in doc.DataWarnings)
                    c.Item().Text("• " + w).FontSize(8);
            });
    }

    private static void ComposeTable(IContainer container, FinancialDocumentDto doc, int decimals)
    {
        var colCount = doc.Columns.Count;

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(colCount > 6 ? 2.6f : 3.4f); // kolumna z nazwą pozycji
                foreach (var _ in doc.Columns)
                    columns.RelativeColumn(colCount > 6 ? 1f : 1.4f);
            });

            // Nagłówek
            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                    .Text("Pozycja").Bold().FontSize(8.5f);
                foreach (var c in doc.Columns)
                {
                    var bg = c.Kind == StatementColumnKind.Total ? Colors.Blue.Lighten4 : Colors.Grey.Lighten3;
                    header.Cell().Background(bg).Padding(5).AlignRight()
                        .Text(c.Label).Bold().FontSize(8.5f);
                }
            });

            // Wiersze
            foreach (var row in doc.Rows)
            {
                if (row.Style == StatementRowStyle.Section)
                {
                    table.Cell().ColumnSpan((uint)(colCount + 1))
                        .Background(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(5)
                        .Text(row.Label).Bold().FontSize(9);
                    continue;
                }

                var rowBg = row.Style == StatementRowStyle.Total ? Colors.Blue.Lighten5 : Colors.White;

                // Komórka z nazwą pozycji (+ wcięcie i opis)
                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                    .PaddingVertical(3.5f).PaddingRight(6).PaddingLeft(6 + row.Indent * 12)
                    .Column(c =>
                    {
                        var label = c.Item().Text(row.Label).FontSize(row.Style == StatementRowStyle.Memo ? 8f : 9f);
                        switch (row.Style)
                        {
                            case StatementRowStyle.Total: label.Bold(); break;
                            case StatementRowStyle.Subtotal: label.SemiBold(); break;
                            case StatementRowStyle.Memo: label.Italic().FontColor(Colors.Grey.Darken1); break;
                        }
                        if (!string.IsNullOrWhiteSpace(row.Description))
                            c.Item().Text(row.Description!).FontSize(6.5f).FontColor(Colors.Grey.Darken1);
                    });

                // Komórki wartości
                for (var i = 0; i < colCount; i++)
                {
                    var v = i < row.Values.Count ? row.Values[i] : null;
                    var cell = table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(3.5f).PaddingHorizontal(5).AlignRight().AlignMiddle()
                        .Text(FormatValue(v, row.IsPercent, decimals));

                    cell.FontSize(row.Style == StatementRowStyle.Memo ? 8f : 9f);
                    switch (row.Style)
                    {
                        case StatementRowStyle.Total: cell.Bold(); break;
                        case StatementRowStyle.Subtotal: cell.SemiBold(); break;
                        case StatementRowStyle.Memo: cell.Italic(); break;
                    }
                    if (v is < 0m)
                        cell.FontColor(Colors.Red.Darken1);
                    else if (row.Style == StatementRowStyle.Memo)
                        cell.FontColor(Colors.Grey.Darken1);
                }
            }
        });
    }

    private static void ComposeNotes(IContainer container, FinancialDocumentDto doc)
    {
        container.PaddingTop(2).Column(col =>
        {
            col.Item().Text("Założenia i zastrzeżenia").Bold().FontSize(8.5f);
            foreach (var note in doc.Notes)
                col.Item().Text("• " + note).FontSize(7.5f).FontColor(Colors.Grey.Darken2);
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(
                    $"Wygenerowano {DateTime.Now.ToString("d MMMM yyyy, HH:mm", Pl)} — ContractorApp")
                .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
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
