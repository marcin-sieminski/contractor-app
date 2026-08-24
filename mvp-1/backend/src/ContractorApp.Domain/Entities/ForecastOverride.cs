using ContractorApp.Domain.Common;

namespace ContractorApp.Domain.Entities;

/// <summary>
/// Trwała ręczna korekta prognozy finansowej dla konkretnego miesiąca danego roku.
/// Zapisywana per użytkownik; nadpisuje wartość run-rate / carry-over w module Analiza finansowa.
/// Dotyczy wyłącznie miesięcy prognozy — miesiące z danymi rzeczywistymi są ignorowane.
/// </summary>
public class ForecastOverride : Entity
{
    public string UserId { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }           // 1..12

    /// <summary>Skorygowany przychód netto (PLN); null = brak korekty przychodu.</summary>
    public decimal? Revenue { get; set; }

    /// <summary>Skorygowany koszt brutto (PLN); null = brak korekty kosztów.</summary>
    public decimal? Cost { get; set; }
}
