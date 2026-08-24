using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Documents;

/// <summary>
/// Wspólna logika składania <see cref="FinancialDocumentDto"/> dla modułu Dokumenty.
/// Agreguje przychody (faktury netto, bez Draft) i koszty (wydatki wg kategorii) per miesiąc
/// — te same reguły co prognoza/rozliczenie: przeliczenie walut kursem z faktury, faktury
/// walutowe bez kursu liczone 1:1 i raportowane do ostrzeżeń. Używana przez query JSON i PDF.
/// </summary>
public static class FinancialStatementBuilder
{
    private static readonly string[] MonthShort =
        { "", "Sty", "Lut", "Mar", "Kwi", "Maj", "Cze", "Lip", "Sie", "Wrz", "Paź", "Lis", "Gru" };

    // Kolejność i polskie nazwy kategorii kosztów na potrzeby rachunku wyników.
    private static readonly (ExpenseCategory Cat, string Label)[] CategoryOrder =
    {
        (ExpenseCategory.Software, "Oprogramowanie"),
        (ExpenseCategory.Hardware, "Sprzęt"),
        (ExpenseCategory.Office, "Biuro i wyposażenie"),
        (ExpenseCategory.Training, "Szkolenia i kursy"),
        (ExpenseCategory.Travel, "Podróże"),
        (ExpenseCategory.Phone, "Telefon i internet"),
        (ExpenseCategory.Insurance, "Ubezpieczenia"),
        (ExpenseCategory.Accounting, "Księgowość"),
        (ExpenseCategory.Marketing, "Marketing"),
        (ExpenseCategory.Literature, "Literatura"),
        (ExpenseCategory.Other, "Pozostałe"),
    };

    public static async Task<FinancialDocumentDto> BuildAsync(
        IApplicationDbContext db,
        string userId,
        int year,
        StatementType type,
        StatementGranularity gran,
        CancellationToken ct)
    {
        var invoices = await db.Invoices
            .Include(i => i.Client)
            .Where(i => i.Client.UserId == userId
                     && i.IssueDate.Year == year
                     && i.Status != InvoiceStatus.Draft)
            .Select(i => new { i.InvoiceNumber, i.IssueDate, i.TotalNet, i.Currency, i.ExchangeRate })
            .ToListAsync(ct);

        var expenses = await db.Expenses
            .Where(e => e.UserId == userId && e.Date.Year == year)
            .Select(e => new { e.Date, e.AmountPLN, e.Category })
            .ToListAsync(ct);

        // Przychód netto per miesiąc (indeks 1..12) + ostrzeżenia o brakującym kursie.
        var revenue = new decimal[13];
        var missingRate = new List<string>();
        foreach (var i in invoices)
        {
            if (i.Currency != Currency.PLN && i.ExchangeRate is null)
                missingRate.Add(i.InvoiceNumber);
            revenue[i.IssueDate.Month] += i.Currency == Currency.PLN
                ? i.TotalNet
                : i.TotalNet * (i.ExchangeRate ?? 1m);
        }

        // Koszty per kategoria per miesiąc + suma kosztów per miesiąc.
        var costByCat = new Dictionary<ExpenseCategory, decimal[]>();
        var totalCost = new decimal[13];
        foreach (var e in expenses)
        {
            if (!costByCat.TryGetValue(e.Category, out var arr))
                costByCat[e.Category] = arr = new decimal[13];
            arr[e.Date.Month] += e.AmountPLN;
            totalCost[e.Date.Month] += e.AmountPLN;
        }

        var hasData = invoices.Count > 0 || expenses.Count > 0;
        var warnings = new List<string>();
        if (missingRate.Count > 0)
            warnings.Add("Faktury walutowe bez kursu NBP (przeliczone 1:1 — uzupełnij kurs): "
                         + string.Join(", ", missingRate));

        return Compose(year, type, gran, revenue, totalCost, costByCat, hasData, warnings);
    }

    /// <summary>
    /// Składa dokument z gotowych agregatów (przychód i koszty per miesiąc, indeks 1..12).
    /// Wydzielone z <see cref="BuildAsync"/> — pozwala testować logikę bez bazy danych.
    /// </summary>
    public static FinancialDocumentDto Compose(
        int year, StatementType type, StatementGranularity gran,
        decimal[] revenue, decimal[] totalCost,
        IReadOnlyDictionary<ExpenseCategory, decimal[]> costByCat,
        bool hasData, IReadOnlyList<string> warnings)
        => type == StatementType.BalanceSheet
            ? BuildBalanceSheet(year, gran, revenue, totalCost, hasData, warnings)
            : BuildIncomeStatement(year, gran, revenue, totalCost, costByCat, hasData, warnings);

    // ── Rachunek wyników (przepływy: sumy okresów) ──────────────────────────────

    private static FinancialDocumentDto BuildIncomeStatement(
        int year, StatementGranularity gran,
        decimal[] revenue, decimal[] totalCost,
        IReadOnlyDictionary<ExpenseCategory, decimal[]> costByCat,
        bool hasData, IReadOnlyList<string> warnings)
    {
        var cols = IncomeColumns(year, gran);
        var columns = cols.Select(c => c.Column).ToList();

        decimal?[] RowValues(decimal[] monthly) =>
            cols.Select(c => (decimal?)Round(c.Reduce(monthly))).ToArray();

        var resultByMonth = new decimal[13];
        for (var m = 1; m <= 12; m++)
            resultByMonth[m] = revenue[m] - totalCost[m];

        var revVals = RowValues(revenue);
        var resVals = RowValues(resultByMonth);

        var rows = new List<StatementRowDto>
        {
            new("revenue", "Przychody netto ze sprzedaży", 0, StatementRowStyle.Subtotal, revVals,
                "Suma kwot netto z faktur (bez wersji roboczych) wg daty wystawienia"),
            new("costs", "Koszty działalności, w tym:", 0, StatementRowStyle.Subtotal,
                RowValues(totalCost),
                "Zarejestrowane wydatki (kwoty brutto w PLN) wg daty poniesienia"),
        };

        // Kategorie kosztów z jakimikolwiek danymi w roku — w ustalonej kolejności.
        foreach (var (cat, label) in CategoryOrder)
        {
            if (costByCat.TryGetValue(cat, out var arr) && Sum(arr, 1, 12) != 0m)
                rows.Add(new($"cost_{cat}", label, 1, StatementRowStyle.Item, RowValues(arr)));
        }

        rows.Add(new("result", "Zysk/strata ze sprzedaży", 0, StatementRowStyle.Total, resVals,
            "Przychody netto pomniejszone o koszty działalności"));

        // Marża = wynik / przychód (liczona z wartości kolumn, nie z sumy ułamków).
        var marginVals = columns.Select((_, idx) =>
        {
            var rev = revVals[idx];
            return rev is null or 0m ? (decimal?)null : Round2(resVals[idx]!.Value / rev.Value * 100m);
        }).ToArray();
        rows.Add(new("margin", "Marża (wynik / przychód)", 0, StatementRowStyle.Memo, marginVals,
            IsPercent: true));

        var notes = new List<string>
        {
            "Przychody = suma kwot netto z faktur (bez wersji roboczych) wg daty wystawienia; "
                + "faktury walutowe przeliczone kursem z faktury.",
            "Koszty = zarejestrowane wydatki (kwoty brutto w PLN) wg daty poniesienia, w podziale na kategorie.",
            "Składki ZUS oraz podatek dochodowy (PIT) nie są ujęte jako koszt — w działalności JDG obciążają "
                + "właściciela. Zobacz moduły Rozliczenie roczne i Analiza finansowa.",
            "Dokument pomocniczy (zestawienie zarządcze) — nie jest sprawozdaniem finansowym w rozumieniu "
                + "ustawy o rachunkowości. Skonsultuj z księgową.",
        };

        return new FinancialDocumentDto(
            year, StatementKinds.TypeKey(StatementType.IncomeStatement),
            StatementKinds.TypeLabel(StatementType.IncomeStatement),
            StatementKinds.GranularityKey(gran),
            $"Rachunek wyników za rok {year}",
            columns, rows, notes, warnings, hasData);
    }

    // ── Bilans uproszczony (stan: migawki narastająco na koniec okresu) ─────────

    private static FinancialDocumentDto BuildBalanceSheet(
        int year, StatementGranularity gran,
        decimal[] revenue, decimal[] totalCost,
        bool hasData, IReadOnlyList<string> warnings)
    {
        // Skumulowany wynik finansowy = środki pieniężne (szac.) = kapitał własny.
        var cumResult = new decimal[13];
        var running = 0m;
        for (var m = 1; m <= 12; m++)
        {
            running += revenue[m] - totalCost[m];
            cumResult[m] = running;
        }

        var snaps = BalanceColumns(year, gran);
        var columns = snaps.Select(s => s.Column).ToList();

        decimal?[] Snap(Func<int, decimal> f) =>
            snaps.Select(s => (decimal?)Round(f(s.Month))).ToArray();

        var cash = Snap(m => cumResult[m]);
        var zero = Snap(_ => 0m);

        var rows = new List<StatementRowDto>
        {
            new("aktywa", "AKTYWA", 0, StatementRowStyle.Section, Array.Empty<decimal?>()),
            new("aktywa_trwale", "A. Aktywa trwałe", 0, StatementRowStyle.Item, zero,
                "Zakupy sprzętu rozliczane w kosztach — brak ewidencji środków trwałych"),
            new("aktywa_obrotowe", "B. Aktywa obrotowe", 0, StatementRowStyle.Subtotal, cash),
            new("srodki_pieniezne", "Środki pieniężne (szacunkowe)", 1, StatementRowStyle.Item, cash,
                "Skumulowany wynik finansowy narastająco na koniec okresu"),
            new("aktywa_razem", "Aktywa razem", 0, StatementRowStyle.Total, cash),

            new("pasywa", "PASYWA", 0, StatementRowStyle.Section, Array.Empty<decimal?>()),
            new("kapital_wlasny", "A. Kapitał (fundusz) własny", 0, StatementRowStyle.Subtotal, cash),
            new("wynik_finansowy", "Wynik finansowy (narastająco)", 1, StatementRowStyle.Item, cash,
                "Suma przychodów pomniejszona o koszty od początku roku"),
            new("zobowiazania", "B. Zobowiązania i rezerwy na zobowiązania", 0, StatementRowStyle.Item, zero,
                "Zobowiązania publicznoprawne (PIT/ZUS/VAT) i handlowe nieewidencjonowane w aplikacji"),
            new("pasywa_razem", "Pasywa razem", 0, StatementRowStyle.Total, cash),
        };

        var notes = new List<string>
        {
            "Uproszczone zestawienie majątkowe: działalność JDG prowadząca KPiR/ewidencję przychodów nie "
                + "sporządza bilansu w rozumieniu ustawy o rachunkowości — poniższe pozycje mają charakter pomocniczy.",
            $"Bilans otwarcia na 1 stycznia {year} przyjęto jako zerowy; pozycje liczone narastająco w obrębie roku.",
            "Środki pieniężne oszacowano jako skumulowany wynik finansowy (przychody − koszty) przy założeniu, "
                + "że należności zostały opłacone, a koszty poniesione w gotówce; ujemna wartość oznacza, "
                + "że działalność per saldo zużyła środki (finansowanie z kapitału właściciela).",
            "Środki trwałe, należności oraz zobowiązania publicznoprawne (PIT/ZUS/VAT) nie są ewidencjonowane "
                + "w aplikacji i przyjęto je jako zerowe; z konstrukcji aktywa równają się pasywom.",
            "Dokument pomocniczy — skonsultuj z księgową.",
        };

        return new FinancialDocumentDto(
            year, StatementKinds.TypeKey(StatementType.BalanceSheet),
            StatementKinds.TypeLabel(StatementType.BalanceSheet),
            StatementKinds.GranularityKey(gran),
            $"Bilans uproszczony — {year}",
            columns, rows, notes, warnings, hasData);
    }

    // ── Kolumny ─────────────────────────────────────────────────────────────────

    private static List<(StatementColumnDto Column, Func<decimal[], decimal> Reduce)> IncomeColumns(
        int year, StatementGranularity gran)
    {
        switch (gran)
        {
            case StatementGranularity.Annual:
                return new()
                {
                    (new("rok", $"Rok {year}", StatementColumnKind.Total), a => Sum(a, 1, 12)),
                };
            case StatementGranularity.Quarterly:
                return new()
                {
                    (new("q1", "I kwartał", StatementColumnKind.Period), a => Sum(a, 1, 3)),
                    (new("q2", "II kwartał", StatementColumnKind.Period), a => Sum(a, 4, 6)),
                    (new("q3", "III kwartał", StatementColumnKind.Period), a => Sum(a, 7, 9)),
                    (new("q4", "IV kwartał", StatementColumnKind.Period), a => Sum(a, 10, 12)),
                    (new("rok", "Rok", StatementColumnKind.Total), a => Sum(a, 1, 12)),
                };
            default:
                var cols = new List<(StatementColumnDto, Func<decimal[], decimal>)>(13);
                for (var m = 1; m <= 12; m++)
                {
                    var month = m;
                    cols.Add((new($"m{month}", MonthShort[month], StatementColumnKind.Period),
                        a => a[month]));
                }
                cols.Add((new("rok", "Rok", StatementColumnKind.Total), a => Sum(a, 1, 12)));
                return cols;
        }
    }

    private static List<(StatementColumnDto Column, int Month)> BalanceColumns(
        int year, StatementGranularity gran)
    {
        switch (gran)
        {
            case StatementGranularity.Annual:
                return new() { (new("k12", $"na 31.12.{year}", StatementColumnKind.Period), 12) };
            case StatementGranularity.Quarterly:
                return new()
                {
                    (new("k3", "na 31.03", StatementColumnKind.Period), 3),
                    (new("k6", "na 30.06", StatementColumnKind.Period), 6),
                    (new("k9", "na 30.09", StatementColumnKind.Period), 9),
                    (new("k12", "na 31.12", StatementColumnKind.Period), 12),
                };
            default:
                var cols = new List<(StatementColumnDto, int)>(12);
                for (var m = 1; m <= 12; m++)
                    cols.Add((new($"k{m}", MonthShort[m], StatementColumnKind.Period), m));
                return cols;
        }
    }

    private static decimal Sum(decimal[] arr, int from, int to)
    {
        var total = 0m;
        for (var i = from; i <= to && i < arr.Length; i++) total += arr[i];
        return total;
    }

    private static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
    private static decimal Round2(decimal v) => Math.Round(v, 1, MidpointRounding.AwayFromZero);
}
