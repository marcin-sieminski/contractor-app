using ContractorApp.Application.Features.Documents;
using ContractorApp.Domain.Enums;
using ContractorApp.Infrastructure.Services.Pdf;
using QuestPDF.Infrastructure;
using Xunit;

namespace ContractorApp.Application.Tests.Pdf;

/// <summary>
/// Smoke-test generatora PDF dokumentów: realna ścieżka builder → PDF dla obu rodzajów
/// dokumentów i wszystkich granulacji (w tym ujęcie miesięczne = 13 kolumn w poziomie),
/// na danych zyskownych i stratnych. Wykrywa wyjątki układu QuestPDF, których kompilacja nie łapie.
/// </summary>
public class DocumentPdfGeneratorTests
{
    static DocumentPdfGeneratorTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static decimal[] Flat(decimal monthly)
    {
        var a = new decimal[13];
        for (var m = 1; m <= 12; m++) a[m] = monthly;
        return a;
    }

    private static FinancialDocumentDto Doc(StatementType type, StatementGranularity gran, decimal rev, decimal cost)
    {
        var costByCat = new Dictionary<ExpenseCategory, decimal[]>
        {
            [ExpenseCategory.Software] = Flat(cost * 0.4m),
            [ExpenseCategory.Hardware] = Flat(cost * 0.3m),
            [ExpenseCategory.Accounting] = Flat(cost * 0.3m),
        };
        return FinancialStatementBuilder.Compose(
            2025, type, gran, Flat(rev), Flat(cost), costByCat, hasData: true,
            warnings: ["Przykładowe ostrzeżenie o danych."]);
    }

    [Theory]
    [InlineData(StatementType.IncomeStatement, StatementGranularity.Monthly)]
    [InlineData(StatementType.IncomeStatement, StatementGranularity.Quarterly)]
    [InlineData(StatementType.IncomeStatement, StatementGranularity.Annual)]
    [InlineData(StatementType.BalanceSheet, StatementGranularity.Monthly)]
    [InlineData(StatementType.BalanceSheet, StatementGranularity.Quarterly)]
    [InlineData(StatementType.BalanceSheet, StatementGranularity.Annual)]
    public void Generuje_niepusty_pdf(StatementType type, StatementGranularity gran)
    {
        var pdf = new DocumentPdfGenerator().Generate(Doc(type, gran, rev: 18_500m, cost: 6_250m));

        Assert.True(pdf.Length > 2_000, $"PDF ma tylko {pdf.Length} bajtów");
        Assert.Equal((byte)'%', pdf[0]); // nagłówek %PDF
        Assert.Equal((byte)'P', pdf[1]);
    }

    [Fact]
    public void Generuje_pdf_dla_scenariusza_ze_strata()
    {
        // Wartości ujemne (strata) — sprawdza renderowanie kolorowanych kwot ujemnych.
        var pdf = new DocumentPdfGenerator().Generate(
            Doc(StatementType.IncomeStatement, StatementGranularity.Monthly, rev: 2_000m, cost: 9_000m));

        Assert.True(pdf.Length > 2_000);
    }
}
