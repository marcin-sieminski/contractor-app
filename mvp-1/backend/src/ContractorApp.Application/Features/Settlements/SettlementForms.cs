using ContractorApp.Domain.Enums;

namespace ContractorApp.Application.Features.Settlements;

/// <summary>Parsowanie i etykiety form opodatkowania — ta sama konwencja co prognoza.</summary>
public static class SettlementForms
{
    public static TaxForm Parse(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "ryczalt" or "ryczałt" => TaxForm.Ryczalt,
        "skala" => TaxForm.Skala,
        _ => TaxForm.Liniowy
    };

    public static ZusStage ParseStage(string? value) => value?.Trim().ToLowerInvariant().Replace("-", "_") switch
    {
        "ulga_na_start" => ZusStage.UlgaNaStart,
        "preferencyjny" => ZusStage.Preferencyjny,
        _ => ZusStage.Pelny
    };

    public static string Key(TaxForm form) => form switch
    {
        TaxForm.Ryczalt => "ryczalt",
        TaxForm.Skala => "skala",
        _ => "liniowy"
    };

    public static string Label(TaxForm form) => form switch
    {
        TaxForm.Ryczalt => "Ryczałt 12%",
        TaxForm.Skala => "Skala 12%/32%",
        _ => "Liniowy 19%"
    };

    /// <summary>Kod formularza zeznania rocznego dla formy opodatkowania.</summary>
    public static string Code(TaxForm form) => form switch
    {
        TaxForm.Ryczalt => "PIT-28",
        TaxForm.Skala => "PIT-36",
        _ => "PIT-36L"
    };

    /// <summary>Walidacja sumy kontrolnej NIP (10 cyfr, wagi 6-5-7-2-3-4-5-6-7).</summary>
    public static bool IsValidNip(string? nip)
    {
        if (string.IsNullOrWhiteSpace(nip)) return false;
        var digits = nip.Where(char.IsDigit).ToArray();
        if (digits.Length != 10) return false;

        int[] weights = [6, 5, 7, 2, 3, 4, 5, 6, 7];
        var sum = weights.Select((w, i) => w * (digits[i] - '0')).Sum();
        var checksum = sum % 11;
        return checksum != 10 && checksum == digits[9] - '0';
    }
}
