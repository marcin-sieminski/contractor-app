using System.Text.Json;
using ContractorApp.Application.Common.Aggregation;
using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.Common.Tax;
using ContractorApp.Domain.Entities;
using ContractorApp.Domain.Enums;

namespace ContractorApp.Application.Features.Settlements;

/// <summary>Zamrożony wynik rozliczenia zapisywany w AnnualSettlement.CalculationJson.</summary>
public sealed record SettlementSnapshot(
    SettlementPrefillDto Prefill,
    SettlementResultDto Result,
    IReadOnlyList<SettlementLineDto> Lines,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> DataWarnings,
    bool IsYearParamsExact);

/// <summary>
/// Wspólna logika składania <see cref="AnnualSettlementDto"/>: agregacja danych,
/// podpowiedzi teoretyczne, uruchomienie kalkulatora, obsługa snapshotu Final.
/// Używana przez Get/Save/Finalize oraz generatory XML/PDF.
/// </summary>
public static class SettlementAssembler
{
    public static async Task<AnnualSettlementDto> BuildAsync(
        IApplicationDbContext db,
        string userId,
        int year,
        TaxForm form,
        ZusStage zusStage,
        AnnualSettlement? entity,
        CancellationToken ct)
    {
        // Zatwierdzone rozliczenie renderowane wyłącznie ze snapshotu —
        // zmiany faktur/wydatków po zatwierdzeniu nie wpływają na wynik.
        if (entity is { Status: SettlementStatus.Final, CalculationJson: not null })
        {
            var snapshot = JsonSerializer.Deserialize<SettlementSnapshot>(entity.CalculationJson)!;
            return ToDto(year, form, entity, snapshot);
        }

        var (parameters, isExact) = TaxYearParametersProvider.ForYear(year);
        var financials = await AnnualFinancialsLoader.LoadAsync(db, userId, year, ct);
        var prefill = BuildPrefill(form, zusStage, parameters, financials);

        var adjustments = entity is null
            ? new SettlementAdjustmentsDto(
                null, null,
                prefill.ZusSocialTheoretical, prefill.ZusHealthTheoretical, prefill.TaxPrepaymentsTheoretical,
                IpBoxEnabled: false, IpQualifyingPercent: 100m, NexusCoefficient: 1m)
            : new SettlementAdjustmentsDto(
                entity.RevenueOverride, entity.CostsOverride,
                entity.ZusSocialPaid, entity.ZusHealthPaid, entity.TaxPrepaymentsPaid,
                entity.IpBoxEnabled, entity.IpQualifyingPercent, entity.NexusCoefficient);

        var calculation = AnnualSettlementCalculator.Calculate(new SettlementCalculationInput(
            year, form, parameters,
            Revenue: adjustments.RevenueOverride ?? financials.AnnualRevenue,
            Costs: adjustments.CostsOverride ?? financials.AnnualCosts,
            ZusSocialPaid: adjustments.ZusSocialPaid,
            ZusHealthPaid: adjustments.ZusHealthPaid,
            TaxPrepaymentsPaid: adjustments.TaxPrepaymentsPaid,
            IpBoxEnabled: adjustments.IpBoxEnabled,
            IpQualifyingPercent: adjustments.IpQualifyingPercent,
            NexusCoefficient: adjustments.NexusCoefficient));

        var warnings = BuildWarnings(year, isExact, financials);
        var snapshot2 = new SettlementSnapshot(
            prefill, ToResultDto(calculation),
            calculation.Lines.Select(l => new SettlementLineDto(l.BoxId, l.Label, l.Value, l.Description)).ToList(),
            calculation.Notes.ToList(), warnings, isExact);

        return ToDto(year, form, entity, snapshot2, adjustments);
    }

    /// <summary>Serializuje bieżący wynik do snapshotu (wywoływane przy zatwierdzaniu).</summary>
    public static string SerializeSnapshot(AnnualSettlementDto dto) =>
        JsonSerializer.Serialize(new SettlementSnapshot(
            dto.Prefill, dto.Result, dto.Lines, dto.Notes, dto.DataWarnings, dto.IsYearParamsExact));

    // ── Wartości teoretyczne (podpowiedzi do pól „zapłacone") ───────────────────

    private static SettlementPrefillDto BuildPrefill(
        TaxForm form, ZusStage zusStage, TaxYearParameters p, AnnualFinancials f)
    {
        var socialMonthly = zusStage switch
        {
            ZusStage.UlgaNaStart => 0m,
            ZusStage.Preferencyjny => p.ZusSocialPreferentialMonthly,
            _ => p.ZusSocialFullMonthly
        };
        var socialAnnual = socialMonthly * 12m;

        decimal healthAnnual;
        if (form == TaxForm.Ryczalt)
        {
            var tier = f.AnnualRevenue switch
            {
                <= 60_000m => p.HealthRyczaltLowMonthly,
                <= 300_000m => p.HealthRyczaltMidMonthly,
                _ => p.HealthRyczaltHighMonthly
            };
            healthAnnual = tier * 12m;
        }
        else
        {
            var rate = form == TaxForm.Skala ? p.HealthScaleRate : p.HealthLinearRate;
            healthAnnual = 0m;
            for (var m = 1; m <= 12; m++)
            {
                var income = Math.Max(0m, f.RevenueByMonth[m] - f.CostsByMonth[m]);
                healthAnnual += Math.Max(p.HealthMinimumMonthly, income * rate);
            }
        }

        // Zaliczki orientacyjne: jak suma zaliczek w trakcie roku (bez odliczenia zdrowotnej).
        var dochod = Math.Max(0m, f.AnnualRevenue - f.AnnualCosts);
        var prepayments = form switch
        {
            TaxForm.Ryczalt => AnnualSettlementCalculator.RoundPln(
                Math.Max(0m, f.AnnualRevenue - socialAnnual - healthAnnual * p.RyczaltHealthDeductibleShare)
                * p.RyczaltItRate),
            TaxForm.Liniowy => AnnualSettlementCalculator.RoundPln(
                Math.Max(0m, dochod - socialAnnual) * p.LinearPitRate),
            _ => AnnualSettlementCalculator.ScaleTax(
                AnnualSettlementCalculator.RoundPln(Math.Max(0m, dochod - socialAnnual)), p)
        };

        return new SettlementPrefillDto(
            Math.Round(f.AnnualRevenue, 2),
            Math.Round(f.AnnualCosts, 2),
            Math.Round(socialAnnual, 2),
            Math.Round(healthAnnual, 2),
            prepayments);
    }

    private static List<string> BuildWarnings(int year, bool isExact, AnnualFinancials f)
    {
        var warnings = new List<string>();
        if (!isExact)
            warnings.Add($"Brak tabeli stawek dla roku {year} — użyto najbliższej dostępnej. "
                         + "Wynik traktuj jako orientacyjny.");
        if (f.InvoicesMissingExchangeRate.Count > 0)
            warnings.Add("Faktury walutowe bez kursu NBP (przeliczone 1:1 — uzupełnij kurs przed zatwierdzeniem): "
                         + string.Join(", ", f.InvoicesMissingExchangeRate));
        return warnings;
    }

    // ── Mapowanie ───────────────────────────────────────────────────────────────

    private static SettlementResultDto ToResultDto(SettlementCalculation c) => new(
        c.Przychod, c.Koszty, c.Dochod, c.Strata,
        c.SkladkiSpoleczneOdliczone, c.SkladkaZdrowotnaOdliczona,
        c.PodstawaOpodatkowania, c.PodstawaIpBox, c.PodatekIpBox, c.PodatekPozaIpBox,
        c.PodatekNalezny, c.ZaliczkiWplacone, c.DoZaplaty, c.Nadplata,
        c.EfektywnaStawkaOdPrzychodu);

    private static AnnualSettlementDto ToDto(
        int year, TaxForm form, AnnualSettlement? entity, SettlementSnapshot snapshot,
        SettlementAdjustmentsDto? adjustments = null) => new(
        year,
        SettlementForms.Key(form),
        SettlementForms.Label(form),
        SettlementForms.Code(form),
        entity?.Status == SettlementStatus.Final ? "final" : "draft",
        entity is not null,
        entity?.FinalizedAt,
        snapshot.IsYearParamsExact,
        snapshot.Prefill,
        adjustments ?? new SettlementAdjustmentsDto(
            entity?.RevenueOverride, entity?.CostsOverride,
            entity?.ZusSocialPaid ?? 0m, entity?.ZusHealthPaid ?? 0m, entity?.TaxPrepaymentsPaid ?? 0m,
            entity?.IpBoxEnabled ?? false, entity?.IpQualifyingPercent ?? 100m, entity?.NexusCoefficient ?? 1m),
        new TaxpayerDataDto(
            entity?.TaxpayerNip, entity?.FirstName, entity?.LastName, entity?.BirthDate,
            entity?.Street, entity?.BuildingNumber, entity?.ApartmentNumber,
            entity?.PostalCode, entity?.City, entity?.TaxOfficeCode,
            entity?.Voivodeship, entity?.County, entity?.Commune),
        snapshot.Result,
        snapshot.Lines,
        snapshot.Notes,
        snapshot.DataWarnings);
}
