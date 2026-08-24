import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CopyPlus, Eraser, Pencil, RotateCcw, TrendingUp } from 'lucide-react'
import { getFinancialForecast, saveForecastOverrides } from '../api/analytics'
import type { ForecastOverrideItem } from '../api/analytics'
import { ForecastChart } from '../components/forecast/ForecastChart'
import { IpBoxPanel } from '../components/forecast/IpBoxPanel'
import { TAX_FORM_LABELS, ZUS_STAGE_LABELS } from '../types/forecast'
import type { TaxFormKey, ZusStageKey } from '../types/forecast'

const fmt = (v: number) =>
  v.toLocaleString('pl-PL', { maximumFractionDigits: 0 }) + ' PLN'

const TAX_FORMS = Object.keys(TAX_FORM_LABELS) as TaxFormKey[]
const ZUS_STAGES = Object.keys(ZUS_STAGE_LABELS) as ZusStageKey[]

const selectCls = "mt-1 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm text-gray-900 dark:text-gray-100 bg-white dark:bg-gray-800"

// Słownik niezapisanych korekt {miesiąc: kwota} → "7:15000,8:16000" dla query API (podgląd na żywo).
type Overrides = Record<number, number>
const serializeOverrides = (o: Overrides): string | undefined => {
  const s = Object.entries(o).map(([m, v]) => `${m}:${v}`).join(',')
  return s.length > 0 ? s : undefined
}

export function ForecastPage() {
  const currentYear = new Date().getFullYear()
  const qc = useQueryClient()
  const [year, setYear] = useState(currentYear)
  const isFutureYear = year > currentYear
  const [taxForm, setTaxForm] = useState<TaxFormKey>('liniowy')
  const [zusStage, setZusStage] = useState<ZusStageKey>('pelny')
  const [includeForecast, setIncludeForecast] = useState(true)
  const [ipBoxEnabled, setIpBoxEnabled] = useState(false)
  const [ipQualifyingPercent, setIpQualifyingPercent] = useState(100)

  // Ręczna edycja prognozy: niezapisane korekty przychodów/kosztów per miesiąc (podgląd na żywo).
  const [editMode, setEditMode] = useState(false)
  const [revenueOverrides, setRevenueOverrides] = useState<Overrides>({})
  const [costOverrides, setCostOverrides] = useState<Overrides>({})
  const hasOverrides =
    Object.keys(revenueOverrides).length > 0 || Object.keys(costOverrides).length > 0

  // Niezapisane korekty dotyczą konkretnego roku — przy zmianie roku je porzucamy.
  useEffect(() => {
    setRevenueOverrides({})
    setCostOverrides({})
    setEditMode(false)
  }, [year])

  const resetOverrides = () => {
    setRevenueOverrides({})
    setCostOverrides({})
  }

  const updateOverride = (kind: 'revenue' | 'cost', month: number, raw: string) => {
    const setter = kind === 'revenue' ? setRevenueOverrides : setCostOverrides
    setter(prev => {
      const next = { ...prev }
      if (raw.trim() === '') delete next[month]
      else next[month] = Math.max(0, Number(raw))
      return next
    })
  }

  // IP Box dostępny tylko dla liniowego i skali (nie ryczałt).
  const ipBoxApplicable = taxForm === 'liniowy' || taxForm === 'skala'
  const ipBoxOn = ipBoxApplicable && ipBoxEnabled

  const revenueOverridesParam = serializeOverrides(revenueOverrides)
  const costOverridesParam = serializeOverrides(costOverrides)

  const { data, isLoading, isError } = useQuery({
    queryKey: [
      'forecast', year, taxForm, zusStage, includeForecast, ipBoxOn, ipQualifyingPercent,
      revenueOverridesParam, costOverridesParam,
    ],
    queryFn: () => getFinancialForecast({
      year, taxForm, zusStage, includeForecast,
      ipBoxEnabled: ipBoxOn,
      ipQualifyingPercent: ipBoxOn ? ipQualifyingPercent : undefined,
      revenueOverrides: revenueOverridesParam,
      costOverrides: costOverridesParam,
    }),
    placeholderData: prev => prev,
  })

  // Zapis trwałych korekt prognozy w bazie.
  const saveMutation = useMutation({
    mutationFn: (body: { action: 'save' | 'clear' | 'reset'; overrides?: ForecastOverrideItem[] }) =>
      saveForecastOverrides(year, body),
    onSuccess: () => {
      resetOverrides()
      setEditMode(false)
      qc.invalidateQueries({ queryKey: ['forecast'] })
    },
  })

  // "Gotowe" — zapisz edycje na stałe. Wysyłamy pełną parę (przychód, koszt) dla zmienionych
  // miesięcy, używając wartości efektywnej (korekta lokalna lub aktualnie wyświetlana).
  const saveEdits = () => {
    if (!hasOverrides) {
      setEditMode(false)
      return
    }
    const months = new Set<number>([
      ...Object.keys(revenueOverrides).map(Number),
      ...Object.keys(costOverrides).map(Number),
    ])
    const byMonth = new Map(data?.months.map(m => [m.month, m]) ?? [])
    const overrides: ForecastOverrideItem[] = [...months].map(m => ({
      month: m,
      revenue: revenueOverrides[m] ?? byMonth.get(m)?.revenue ?? 0,
      cost: costOverrides[m] ?? byMonth.get(m)?.costs ?? 0,
    }))
    saveMutation.mutate({ action: 'save', overrides })
  }

  // "Wyczyść dane" — trwale wyzeruj prognozę roku.
  const clearForecast = () => saveMutation.mutate({ action: 'clear' })
  // "Przenieś z poprzedniego roku" — usuń korekty, wróć do prognozy automatycznej (carry-over).
  const carryOverFromPreviousYear = () => saveMutation.mutate({ action: 'reset' })

  // Stan bazy wyznaczamy z danych: brak ręcznych miesięcy = carry-over; same zera = wyczyszczone.
  const forecastMonths = data?.months.filter(m => !m.isActual) ?? []
  const anyEdited = forecastMonths.some(m => m.isEdited)
  const isCleared = forecastMonths.length > 0 &&
    forecastMonths.every(m => m.isEdited && m.revenue === 0 && m.costs === 0)

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-2">
          <TrendingUp className="text-blue-600" size={24} />
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Analiza finansowa</h1>
        </div>
      </div>

      <div className="flex flex-wrap items-end gap-3 mb-6">
        <label className="flex flex-col text-xs font-medium text-gray-500 dark:text-gray-400">
          Zakres danych
          <div className="mt-1 flex rounded-lg border border-gray-300 dark:border-gray-600 overflow-hidden text-sm">
            <button
              onClick={() => setIncludeForecast(false)}
              className={`px-3 py-2 transition-colors whitespace-nowrap ${
                !includeForecast
                  ? 'bg-blue-600 text-white'
                  : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'
              }`}
            >
              Tylko rzeczywiste
            </button>
            <button
              onClick={() => setIncludeForecast(true)}
              className={`px-3 py-2 transition-colors whitespace-nowrap border-l border-gray-300 dark:border-gray-600 ${
                includeForecast
                  ? 'bg-blue-600 text-white'
                  : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'
              }`}
            >
              Z prognozą
            </button>
          </div>
        </label>

        <label className="flex flex-col text-xs font-medium text-gray-500 dark:text-gray-400">
          Rok
          <select value={year} onChange={e => setYear(Number(e.target.value))} className={selectCls}>
            {[currentYear - 1, currentYear, currentYear + 1].map(y => (
              <option key={y} value={y}>{y}</option>
            ))}
          </select>
        </label>
        <label className="flex flex-col text-xs font-medium text-gray-500 dark:text-gray-400">
          Forma opodatkowania
          <select value={taxForm} onChange={e => setTaxForm(e.target.value as TaxFormKey)} className={selectCls}>
            {TAX_FORMS.map(f => (
              <option key={f} value={f}>{TAX_FORM_LABELS[f]}</option>
            ))}
          </select>
        </label>
        <label className="flex flex-col text-xs font-medium text-gray-500 dark:text-gray-400">
          Etap ZUS
          <select value={zusStage} onChange={e => setZusStage(e.target.value as ZusStageKey)} className={selectCls}>
            {ZUS_STAGES.map(s => (
              <option key={s} value={s}>{ZUS_STAGE_LABELS[s]}</option>
            ))}
          </select>
        </label>

        {ipBoxApplicable && (
          <>
            <label className="flex flex-col text-xs font-medium text-gray-500 dark:text-gray-400">
              Scenariusz IP Box
              <div className="mt-1 flex rounded-lg border border-gray-300 dark:border-gray-600 overflow-hidden text-sm">
                <button
                  onClick={() => setIpBoxEnabled(false)}
                  className={`px-3 py-2 transition-colors whitespace-nowrap ${
                    !ipBoxEnabled
                      ? 'bg-violet-600 text-white'
                      : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'
                  }`}
                >
                  Wył.
                </button>
                <button
                  onClick={() => setIpBoxEnabled(true)}
                  className={`px-3 py-2 transition-colors whitespace-nowrap border-l border-gray-300 dark:border-gray-600 ${
                    ipBoxEnabled
                      ? 'bg-violet-600 text-white'
                      : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'
                  }`}
                >
                  Włącz
                </button>
              </div>
            </label>

            {ipBoxEnabled && (
              <label className="flex flex-col text-xs font-medium text-gray-500 dark:text-gray-400">
                % dochodu kwalifikowanego (IP)
                <input
                  type="number"
                  min={0}
                  max={100}
                  value={ipQualifyingPercent}
                  onChange={e => setIpQualifyingPercent(Math.min(100, Math.max(0, Number(e.target.value))))}
                  className={`${selectCls} w-32`}
                />
              </label>
            )}
          </>
        )}
      </div>

      {isLoading && <div className="text-gray-500 dark:text-gray-400 text-sm py-12 text-center">Ładowanie…</div>}
      {isError && <div role="alert" className="text-red-500 text-sm py-12 text-center">Nie udało się pobrać danych.</div>}

      {data && (
        <>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
            <Kpi
              label={includeForecast ? 'Prognozowany przychód (rok)' : 'Rzeczywisty przychód'}
              value={fmt(data.fullYear.revenue)}
              color="text-blue-600"
              sub={`śr. ${fmt(data.avgMonthlyRevenue)}/mies.`}
            />
            <Kpi
              label={includeForecast ? 'Prognozowane koszty (rok)' : 'Rzeczywiste koszty'}
              value={fmt(data.fullYear.costs)}
              color="text-red-500"
              sub={`śr. ${fmt(data.avgMonthlyCosts)}/mies.`}
            />
            <Kpi
              label="Obciążenia (PIT+ZUS+VAT)"
              value={fmt(data.fullYear.totalObligations)}
              color="text-amber-600"
              sub={`PIT ${fmt(data.fullYear.incomeTax)} · ZUS ${fmt(data.fullYear.zusSocial + data.fullYear.zusHealth)}`}
            />
            <Kpi
              label="Przepływ netto"
              value={fmt(data.fullYear.netCashFlow)}
              color="text-green-600"
              sub={`VAT do zapłaty ${fmt(data.fullYear.vatPayable)}`}
            />
          </div>

          {data.months.length > 0 && (
            <div className="mb-6">
              <ForecastChart months={data.months} showForecastHint={includeForecast} />
            </div>
          )}

          <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden mb-6">
            <div className="px-4 py-3 border-b border-gray-100 dark:border-gray-700 flex items-center justify-between gap-3">
              <span className="text-sm font-semibold text-gray-700 dark:text-gray-200">
                {includeForecast ? `Prognoza miesięczna ${data.year}` : `Dane rzeczywiste ${data.year}`}
              </span>
              <div className="flex items-center gap-3">
                <span className="text-xs text-gray-500 dark:text-gray-400 hidden sm:inline">
                  {!includeForecast
                    ? data.monthsWithData > 0
                      ? `${data.monthsWithData} mies. z danymi`
                      : 'brak danych rzeczywistych w wybranym roku'
                    : isFutureYear
                      ? isCleared
                        ? 'dane wyzerowane — uzupełnij ręcznie'
                        : `dane rzeczywiste i prognozowane skopiowane z ${year - 1} · możesz je edytować`
                      : data.monthsWithData > 0
                        ? `dane rzeczywiste: ${data.monthsWithData} mies. · pozostałe = prognoza`
                        : 'brak danych — prognoza zerowa'
                  }
                </span>
                {includeForecast && data.monthsWithData < 12 && (
                  <>
                    {isFutureYear && (
                      <>
                        <button
                          onClick={carryOverFromPreviousYear}
                          disabled={saveMutation.isPending}
                          className={`inline-flex items-center gap-1 text-xs px-2 py-1 rounded-lg border transition-colors disabled:opacity-50 ${
                            !anyEdited
                              ? 'bg-blue-600 border-blue-600 text-white'
                              : 'border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'
                          }`}
                          title={`Skopiuj przychody i koszty z ${year - 1}`}
                        >
                          <CopyPlus size={13} aria-hidden="true" /> Przenieś z {year - 1}
                        </button>
                        <button
                          onClick={clearForecast}
                          disabled={saveMutation.isPending}
                          className={`inline-flex items-center gap-1 text-xs px-2 py-1 rounded-lg border transition-colors disabled:opacity-50 ${
                            isCleared
                              ? 'bg-blue-600 border-blue-600 text-white'
                              : 'border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'
                          }`}
                          title="Wyzeruj dane prognozy"
                        >
                          <Eraser size={13} aria-hidden="true" /> Wyczyść dane
                        </button>
                      </>
                    )}
                    {editMode && hasOverrides && (
                      <button
                        onClick={resetOverrides}
                        disabled={saveMutation.isPending}
                        className="inline-flex items-center gap-1 text-xs px-2 py-1 rounded-lg border border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors disabled:opacity-50"
                        title="Porzuć niezapisane zmiany"
                      >
                        <RotateCcw size={13} aria-hidden="true" /> Cofnij zmiany
                      </button>
                    )}
                    <button
                      onClick={() => (editMode ? saveEdits() : setEditMode(true))}
                      disabled={saveMutation.isPending}
                      className={`inline-flex items-center gap-1 text-xs px-2 py-1 rounded-lg border transition-colors disabled:opacity-50 ${
                        editMode
                          ? 'bg-blue-600 border-blue-600 text-white'
                          : 'border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'
                      }`}
                    >
                      <Pencil size={13} aria-hidden="true" /> {editMode ? (saveMutation.isPending ? 'Zapisywanie…' : 'Gotowe') : 'Edytuj prognozę'}
                    </button>
                  </>
                )}
              </div>
            </div>

            {data.months.length === 0 ? (
              <div className="px-4 py-8 text-center text-sm text-gray-500 dark:text-gray-400">
                Brak danych rzeczywistych w wybranym roku.
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="text-xs text-gray-500 dark:text-gray-400 border-b border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
                      <th className="text-left font-medium px-3 py-2">Miesiąc</th>
                      <th className="text-right font-medium px-3 py-2">Przychód</th>
                      <th className="text-right font-medium px-3 py-2">Koszty</th>
                      <th className="text-right font-medium px-3 py-2">PIT</th>
                      <th className="text-right font-medium px-3 py-2">ZUS społ.</th>
                      <th className="text-right font-medium px-3 py-2">Zdrowotna</th>
                      <th className="text-right font-medium px-3 py-2">VAT do zapłaty</th>
                      <th className="text-right font-medium px-3 py-2">Obciążenia</th>
                      <th className="text-right font-medium px-3 py-2">Netto</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.months.map(m => {
                      const editable = editMode && !m.isActual
                      return (
                      <tr
                        key={m.month}
                        className={`border-b border-gray-50 dark:border-gray-700/50 last:border-0 ${m.isActual ? 'text-gray-900 dark:text-gray-100' : 'bg-slate-50/60 dark:bg-gray-900/40 text-gray-500 dark:text-gray-400'} ${m.isEdited ? 'ring-1 ring-inset ring-blue-300 dark:ring-blue-700' : ''}`}
                      >
                        <td className="px-3 py-2 capitalize whitespace-nowrap">
                          {m.monthName}
                          {!m.isActual && (
                            <span className={`ml-2 text-[10px] uppercase tracking-wide rounded px-1 py-0.5 border ${m.isEdited ? 'text-blue-600 border-blue-300 dark:border-blue-700' : 'text-blue-500 border-blue-200 dark:border-blue-800'}`}>
                              {m.isEdited ? 'ręcznie' : 'prognoza'}
                            </span>
                          )}
                        </td>
                        <td className="px-3 py-2 text-right tabular-nums">
                          {editable ? (
                            <input
                              type="number"
                              aria-label={`Przychód — ${m.monthName}`}
                              min={0}
                              value={revenueOverrides[m.month] ?? m.revenue}
                              onChange={e => updateOverride('revenue', m.month, e.target.value)}
                              className="w-28 text-right tabular-nums border border-gray-300 dark:border-gray-600 rounded px-2 py-1 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
                            />
                          ) : fmt(m.revenue)}
                        </td>
                        <td className="px-3 py-2 text-right tabular-nums">
                          {editable ? (
                            <input
                              type="number"
                              aria-label={`Koszty — ${m.monthName}`}
                              min={0}
                              value={costOverrides[m.month] ?? m.costs}
                              onChange={e => updateOverride('cost', m.month, e.target.value)}
                              className="w-28 text-right tabular-nums border border-gray-300 dark:border-gray-600 rounded px-2 py-1 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
                            />
                          ) : fmt(m.costs)}
                        </td>
                        <td className="px-3 py-2 text-right tabular-nums">{fmt(m.incomeTax)}</td>
                        <td className="px-3 py-2 text-right tabular-nums">{fmt(m.zusSocial)}</td>
                        <td className="px-3 py-2 text-right tabular-nums">{fmt(m.zusHealth)}</td>
                        <td className="px-3 py-2 text-right tabular-nums">{fmt(m.vatPayable)}</td>
                        <td className="px-3 py-2 text-right tabular-nums font-medium text-amber-700 dark:text-amber-500">{fmt(m.totalObligations)}</td>
                        <td className={`px-3 py-2 text-right tabular-nums font-medium ${m.netCashFlow >= 0 ? 'text-green-700 dark:text-green-500' : 'text-red-600 dark:text-red-400'}`}>
                          {fmt(m.netCashFlow)}
                        </td>
                      </tr>
                      )
                    })}
                  </tbody>
                  <tfoot>
                    <tr className="border-t-2 border-gray-200 dark:border-gray-600 font-semibold text-gray-800 dark:text-gray-200 bg-gray-50 dark:bg-gray-900">
                      <td className="px-3 py-2">{includeForecast ? 'Razem (rok)' : 'Suma'}</td>
                      <td className="px-3 py-2 text-right tabular-nums">{fmt(data.fullYear.revenue)}</td>
                      <td className="px-3 py-2 text-right tabular-nums">{fmt(data.fullYear.costs)}</td>
                      <td className="px-3 py-2 text-right tabular-nums">{fmt(data.fullYear.incomeTax)}</td>
                      <td className="px-3 py-2 text-right tabular-nums">{fmt(data.fullYear.zusSocial)}</td>
                      <td className="px-3 py-2 text-right tabular-nums">{fmt(data.fullYear.zusHealth)}</td>
                      <td className="px-3 py-2 text-right tabular-nums">{fmt(data.fullYear.vatPayable)}</td>
                      <td className="px-3 py-2 text-right tabular-nums text-amber-700 dark:text-amber-500">{fmt(data.fullYear.totalObligations)}</td>
                      <td className={`px-3 py-2 text-right tabular-nums ${data.fullYear.netCashFlow >= 0 ? 'text-green-700 dark:text-green-500' : 'text-red-600 dark:text-red-400'}`}>
                        {fmt(data.fullYear.netCashFlow)}
                      </td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            )}
          </div>

          {data.ipBox && <IpBoxPanel ipBox={data.ipBox} />}

          {includeForecast && (
            <div className="bg-blue-50/50 dark:bg-blue-900/10 border border-blue-100 dark:border-blue-900 rounded-xl p-4">
              <div className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-2">Założenia prognozy</div>
              <ul className="list-disc list-inside text-xs text-gray-600 dark:text-gray-400 space-y-1">
                {data.assumptions.map((a, i) => <li key={i}>{a}</li>)}
              </ul>
            </div>
          )}
        </>
      )}
    </div>
  )
}

function Kpi({ label, value, color, sub }: { label: string; value: string; color: string; sub?: string }) {
  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
      <div className="text-gray-500 dark:text-gray-400 text-sm mb-1">{label}</div>
      <div className={`text-2xl font-bold ${color}`}>{value}</div>
      {sub && <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">{sub}</div>}
    </div>
  )
}
