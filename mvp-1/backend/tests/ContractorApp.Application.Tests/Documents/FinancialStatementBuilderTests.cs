using ContractorApp.Application.Features.Documents;
using ContractorApp.Domain.Enums;
using Xunit;

namespace ContractorApp.Application.Tests.Documents;

/// <summary>
/// Testy logiki składania zestawień (bez bazy) przez <see cref="FinancialStatementBuilder.Compose"/>:
/// poprawność sum okresów rachunku wyników oraz bilansowanie się aktywów z pasywami.
/// </summary>
public class FinancialStatementBuilderTests
{
    private const int Year = 2025;

    // Tablice indeksowane miesiącem 1..12 (indeks 0 nieużywany).
    private static decimal[] Flat(decimal monthly)
    {
        var a = new decimal[13];
        for (var m = 1; m <= 12; m++) a[m] = monthly;
        return a;
    }

    private static decimal? Cell(FinancialDocumentDto doc, string rowKey, int col)
        => doc.Rows.Single(r => r.Key == rowKey).Values[col];

    private static FinancialDocumentDto Income(StatementGranularity gran, decimal rev, decimal cost)
    {
        var costByCat = new Dictionary<ExpenseCategory, decimal[]>
        {
            [ExpenseCategory.Software] = Flat(cost / 2m),
            [ExpenseCategory.Hardware] = Flat(cost / 2m),
        };
        return FinancialStatementBuilder.Compose(
            Year, StatementType.IncomeStatement, gran, Flat(rev), Flat(cost), costByCat, true, []);
    }

    private static FinancialDocumentDto Balance(StatementGranularity gran, decimal rev, decimal cost)
        => FinancialStatementBuilder.Compose(
            Year, StatementType.BalanceSheet, gran,
            Flat(rev), Flat(cost),
            new Dictionary<ExpenseCategory, decimal[]>(), true, []);

    [Fact]
    public void Rachunek_roczny_sumuje_przychod_koszty_i_wynik()
    {
        var doc = Income(StatementGranularity.Annual, rev: 10_000m, cost: 4_000m);

        Assert.Single(doc.Columns);
        Assert.Equal(StatementColumnKind.Total, doc.Columns[0].Kind);
        Assert.Equal(120_000m, Cell(doc, "revenue", 0));
        Assert.Equal(48_000m, Cell(doc, "costs", 0));
        Assert.Equal(72_000m, Cell(doc, "result", 0));
        Assert.Equal(60m, Cell(doc, "margin", 0)); // 72000 / 120000 = 60%
    }

    [Fact]
    public void Rachunek_kwartalny_ma_cztery_kwartaly_i_kolumne_roczna()
    {
        var doc = Income(StatementGranularity.Quarterly, rev: 10_000m, cost: 4_000m);

        Assert.Equal(5, doc.Columns.Count);
        Assert.Equal(StatementColumnKind.Total, doc.Columns[4].Kind);
        // Każdy kwartał = 3 × 10 000 przychodu.
        for (var q = 0; q < 4; q++)
            Assert.Equal(30_000m, Cell(doc, "revenue", q));
        Assert.Equal(120_000m, Cell(doc, "revenue", 4)); // kolumna roczna
    }

    [Fact]
    public void Rachunek_miesieczny_ma_dwanascie_miesiecy_plus_rok()
    {
        var doc = Income(StatementGranularity.Monthly, rev: 10_000m, cost: 4_000m);

        Assert.Equal(13, doc.Columns.Count);
        Assert.Equal(10_000m, Cell(doc, "revenue", 0));   // styczeń
        Assert.Equal(6_000m, Cell(doc, "result", 11));    // grudzień: 10000 - 4000
        Assert.Equal(120_000m, Cell(doc, "revenue", 12)); // kolumna roczna
    }

    [Theory]
    [InlineData(StatementGranularity.Annual)]
    [InlineData(StatementGranularity.Quarterly)]
    [InlineData(StatementGranularity.Monthly)]
    public void Bilans_zawsze_sie_bilansuje(StatementGranularity gran)
    {
        // Scenariusz zyskowny i stratny — w obu aktywa muszą równać się pasywom w każdej kolumnie.
        foreach (var (rev, cost) in new[] { (10_000m, 4_000m), (1_000m, 5_000m) })
        {
            var doc = Balance(gran, rev, cost);
            for (var c = 0; c < doc.Columns.Count; c++)
                Assert.Equal(Cell(doc, "aktywa_razem", c), Cell(doc, "pasywa_razem", c));
        }
    }

    [Fact]
    public void Bilans_kumuluje_wynik_na_koniec_okresu()
    {
        // Zysk 6 000/mies. → stan narastająco: 18 000 (Q1), 36 000, 54 000, 72 000 (koniec roku).
        var doc = Balance(StatementGranularity.Quarterly, rev: 10_000m, cost: 4_000m);

        Assert.Equal(4, doc.Columns.Count);
        Assert.Equal(18_000m, Cell(doc, "aktywa_razem", 0));
        Assert.Equal(72_000m, Cell(doc, "aktywa_razem", 3));
        Assert.Equal(72_000m, Cell(doc, "kapital_wlasny", 3));
        Assert.Equal(0m, Cell(doc, "zobowiazania", 3));
        Assert.Equal(0m, Cell(doc, "aktywa_trwale", 3));
    }
}
