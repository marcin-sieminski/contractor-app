using ContractorApp.Application.Features.Settlements;
using ContractorApp.Domain.Enums;
using Xunit;

namespace ContractorApp.Application.Tests.Settlements;

/// <summary>
/// Testy SettlementForms: parsowanie formy opodatkowania i etapu ZUS,
/// generowanie kluczy/etykiet/kodów formularzy oraz walidacja sumy kontrolnej NIP.
/// </summary>
public class SettlementFormsTests
{
    // ── Parse (TaxForm) ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("liniowy",  TaxForm.Liniowy)]
    [InlineData("LINIOWY",  TaxForm.Liniowy)]  // case-insensitive
    [InlineData(null,       TaxForm.Liniowy)]  // null → default liniowy
    [InlineData("",         TaxForm.Liniowy)]  // pusty string → default liniowy
    [InlineData("cokolwiek",TaxForm.Liniowy)]  // nieznana wartość → default
    [InlineData("ryczalt",  TaxForm.Ryczalt)]
    [InlineData("ryczałt",  TaxForm.Ryczalt)]  // polska litera
    [InlineData("RYCZALT",  TaxForm.Ryczalt)]
    [InlineData("skala",    TaxForm.Skala)]
    [InlineData("SKALA",    TaxForm.Skala)]
    public void Parse_rozpoznaje_forme_opodatkowania(string? input, TaxForm expected)
        => Assert.Equal(expected, SettlementForms.Parse(input));

    // ── ParseStage (ZusStage) ────────────────────────────────────────────────

    [Theory]
    [InlineData(null,            ZusStage.Pelny)]   // null → default
    [InlineData("pelny",         ZusStage.Pelny)]
    [InlineData("nieznany",      ZusStage.Pelny)]   // nieznana wartość → default
    [InlineData("ulga_na_start", ZusStage.UlgaNaStart)]
    [InlineData("ulga-na-start", ZusStage.UlgaNaStart)]  // myślnik zamiast podkreślenia
    [InlineData("ULGA_NA_START", ZusStage.UlgaNaStart)]
    [InlineData("preferencyjny", ZusStage.Preferencyjny)]
    [InlineData("PREFERENCYJNY", ZusStage.Preferencyjny)]
    public void ParseStage_rozpoznaje_etap_zus(string? input, ZusStage expected)
        => Assert.Equal(expected, SettlementForms.ParseStage(input));

    // ── Key / Label / Code ───────────────────────────────────────────────────

    [Theory]
    [InlineData(TaxForm.Liniowy, "liniowy",    "Liniowy 19%",    "PIT-36L")]
    [InlineData(TaxForm.Skala,   "skala",      "Skala 12%/32%",  "PIT-36")]
    [InlineData(TaxForm.Ryczalt, "ryczalt",    "Ryczałt 12%",    "PIT-28")]
    public void Key_Label_Code_zwracaja_prawidlowe_wartosci(
        TaxForm form, string expectedKey, string expectedLabel, string expectedCode)
    {
        Assert.Equal(expectedKey,   SettlementForms.Key(form));
        Assert.Equal(expectedLabel, SettlementForms.Label(form));
        Assert.Equal(expectedCode,  SettlementForms.Code(form));
    }

    [Fact]
    public void Key_i_Parse_sa_odwrotne()
    {
        // Key(f) → string → Parse() powinno dać z powrotem ten sam TaxForm
        foreach (var form in Enum.GetValues<TaxForm>())
            Assert.Equal(form, SettlementForms.Parse(SettlementForms.Key(form)));
    }

    // ── IsValidNip – poprawne NIPy ────────────────────────────────────────────

    [Theory]
    [InlineData("5252248481")]          // NIP użyty w danych testowych (Apple Polska / example)
    [InlineData("1234563224")]          // suma: 125, 125 % 11 = 4, cyfra kontrolna 4 ✓
    [InlineData("525-224-84-81")]       // z myślnikami → cyfry wyekstrahowane
    [InlineData("NIP: 5252248481")]     // z prefiksem tekstowym
    public void IsValidNip_poprawny_nip(string nip)
        => Assert.True(SettlementForms.IsValidNip(nip));

    // ── IsValidNip – niepoprawne NIPy ─────────────────────────────────────────

    [Theory]
    [InlineData(null)]                  // null
    [InlineData("")]                    // pusty
    [InlineData("   ")]                 // same spacje
    [InlineData("525224848")]           // za krótki (9 cyfr)
    [InlineData("52522484810")]         // za długi (11 cyfr)
    [InlineData("5252248480")]          // błędna cyfra kontrolna (zmieniona ostatnia z 1 na 0)
    [InlineData("5252248482")]          // błędna cyfra kontrolna (zmieniona z 1 na 2)
    [InlineData("abcdefghij")]          // brak cyfr
    public void IsValidNip_bledny_nip(string? nip)
        => Assert.False(SettlementForms.IsValidNip(nip));

    [Fact]
    public void IsValidNip_suma_kontrolna_rowna_10_jest_niedozwolona()
    {
        // NIP 9000000001: suma wag = 6×9 = 54; 54 % 11 = 10 → algorytm odrzuca (checksum != 10).
        // Żadna cyfra dziesiętna nie może równać się 10, więc takie NIPy są formalnie nieważne.
        Assert.False(SettlementForms.IsValidNip("9000000001"));
    }
}
