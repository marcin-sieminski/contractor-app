import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  AreaChart, Area, XAxis, YAxis, Tooltip, ResponsiveContainer,
  CartesianGrid, ReferenceLine,
} from 'recharts'
import { AlertTriangle, CheckCircle, Droplets, TrendingDown } from 'lucide-react'
import { getCashFlowForecast } from '../api/cashFlow'
import type { CashFlowEvent, CashFlowDay } from '../types/cashFlow'
import { TAX_FORM_LABELS, ZUS_STAGE_LABELS } from '../types/forecast'
import type { TaxFormKey, ZusStageKey } from '../types/forecast'

const fmt = (v: number) => v.toLocaleString('pl-PL', { maximumFractionDigits: 0 }) + ' PLN'
const fmtShort = (v: number) => {
  if (Math.abs(v) >= 1000) return (v / 1000).toLocaleString('pl-PL', { maximumFractionDigits: 1 }) + 'k'
  return v.toLocaleString('pl-PL', { maximumFractionDigits: 0 })
}

const STATUS_COLOR: Record<string, string> = {
  ok: 'text-emerald-600 dark:text-emerald-400',
  tight: 'text-amber-600 dark:text-amber-400',
  danger: 'text-red-600 dark:text-red-400',
  overdue: 'text-red-700 dark:text-red-300',
  paid: 'text-gray-500 dark:text-gray-400',
}

const STATUS_BG: Record<string, string> = {
  ok: 'bg-emerald-50 dark:bg-emerald-900/20 border-emerald-200 dark:border-emerald-800',
  tight: 'bg-amber-50 dark:bg-amber-900/20 border-amber-200 dark:border-amber-800',
  danger: 'bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800',
  overdue: 'bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800',
  paid: 'bg-gray-50 dark:bg-gray-800 border-gray-200 dark:border-gray-700',
}

const STATUS_LABEL: Record<string, string> = {
  ok: 'OK',
  tight: 'Uwaga',
  danger: 'Ryzyko',
  overdue: 'Zaległe',
  paid: 'Zapłacone',
}

// Obligation days for ReferenceLine markers
function ObligationMarkers({ days }: { days: CashFlowDay[] }) {
  return (
    <>
      {days
        .filter(d => d.isObligationDay)
        .map(d => (
          <ReferenceLine
            key={d.date}
            x={d.date.slice(5)}  // MM-DD
            stroke={
              d.obligationStatus === 'danger' ? '#ef4444'
              : d.obligationStatus === 'tight' ? '#f59e0b'
              : '#10b981'
            }
            strokeDasharray="4 2"
            strokeWidth={1.5}
          />
        ))}
    </>
  )
}

// Custom tooltip for the area chart
interface TooltipProps {
  active?: boolean
  payload?: Array<{ value: number }>
  label?: string
}

function BalanceTooltip({ active, payload, label }: TooltipProps) {
  if (!active || !payload?.length) return null
  const balance = payload[0].value
  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 shadow-lg text-sm">
      <div className="text-gray-500 dark:text-gray-400 text-xs mb-1">{label}</div>
      <div className={`font-semibold ${balance >= 0 ? 'text-emerald-600 dark:text-emerald-400' : 'text-red-600 dark:text-red-400'}`}>
        {fmt(balance)}
      </div>
    </div>
  )
}

// Event row in the upcoming obligations/inflows list
function EventRow({ event }: { event: CashFlowEvent }) {
  return (
    <div className={`flex items-center justify-between px-4 py-3 border rounded-lg text-sm ${STATUS_BG[event.status]}`}>
      <div className="flex items-center gap-3 min-w-0">
        <span className="text-xs font-mono text-gray-500 dark:text-gray-400 w-20 flex-shrink-0">
          {event.date.slice(5).replace('-', '.')}
        </span>
        <span className={`flex-shrink-0 text-xs font-medium px-1.5 py-0.5 rounded ${
          event.kind === 'invoice'
            ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300'
            : 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-300'
        }`}>
          {event.kind === 'invoice' ? 'faktura' : 'podatek'}
        </span>
        <span className="text-gray-700 dark:text-gray-300 truncate">{event.label}</span>
      </div>
      <div className="flex items-center gap-3 flex-shrink-0 ml-3">
        <span className={`font-semibold ${event.kind === 'invoice' ? 'text-emerald-600 dark:text-emerald-400' : 'text-red-500 dark:text-red-400'}`}>
          {event.kind === 'invoice' ? '+' : '−'}{fmt(event.amount)}
        </span>
        <span className={`text-xs font-medium w-20 text-right ${STATUS_COLOR[event.status]}`}>
          {STATUS_LABEL[event.status]}
        </span>
      </div>
    </div>
  )
}

const TAX_FORMS = Object.keys(TAX_FORM_LABELS) as TaxFormKey[]
const ZUS_STAGES = Object.keys(ZUS_STAGE_LABELS) as ZusStageKey[]

const selectCls = 'border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'

export function CashFlowPage() {
  const [startingBalance, setStartingBalance] = useState(0)
  const [inputBalance, setInputBalance] = useState('0')
  const [taxForm, setTaxForm] = useState<TaxFormKey>('liniowy')
  const [zusStage, setZusStage] = useState<ZusStageKey>('pelny')

  const { data, isLoading, isError } = useQuery({
    queryKey: ['cash-flow', startingBalance, taxForm, zusStage],
    queryFn: () => getCashFlowForecast(startingBalance, taxForm, zusStage),
  })

  // Redukuj 90 punktów do co 3. dnia dla czytelności wykresu
  const chartData = (data?.days ?? [])
    .filter((_, i) => i % 3 === 0 || (data?.days[i]?.isObligationDay))
    .map(d => ({
      date: d.date.slice(5),
      balance: d.balance,
      isObligation: d.isObligationDay,
    }))

  const hasDanger = data?.events.some(e => e.status === 'danger')
  const hasTight = data?.events.some(e => e.status === 'tight')
  const overallStatus = hasDanger ? 'danger' : hasTight ? 'tight' : 'ok'

  const projectedBalance = data?.projectedEndBalance ?? 0

  return (
    <div className="p-6 max-w-5xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-4">
        <div className="flex items-center gap-3">
          <Droplets className="text-blue-600" size={26} />
          <div>
            <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Rolling Cash Flow 90 dni</h1>
            {data && (
              <p className="text-sm text-gray-500 dark:text-gray-400">
                {data.fromDate} → {data.toDate}
              </p>
            )}
          </div>
        </div>

        {/* Parametry */}
        <div className="flex flex-wrap items-center gap-3">
          <select aria-label="Forma opodatkowania" value={taxForm} onChange={e => setTaxForm(e.target.value as TaxFormKey)} className={selectCls}>
            {TAX_FORMS.map(f => <option key={f} value={f}>{TAX_FORM_LABELS[f]}</option>)}
          </select>
          <select aria-label="Etap ZUS" value={zusStage} onChange={e => setZusStage(e.target.value as ZusStageKey)} className={selectCls}>
            {ZUS_STAGES.map(s => <option key={s} value={s}>{ZUS_STAGE_LABELS[s]}</option>)}
          </select>
          <div className="flex items-center gap-2">
            <label className="text-sm text-gray-600 dark:text-gray-300 whitespace-nowrap">Saldo konta:</label>
            <input
              type="number"
              aria-label="Saldo konta (PLN)"
              value={inputBalance}
              onChange={e => setInputBalance(e.target.value)}
              onBlur={() => setStartingBalance(Number(inputBalance) || 0)}
              onKeyDown={e => e.key === 'Enter' && setStartingBalance(Number(inputBalance) || 0)}
              className="w-32 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
              placeholder="0"
            />
            <span className="text-sm text-gray-500 dark:text-gray-400">PLN</span>
          </div>
        </div>
      </div>

      {isLoading && <div className="text-center py-16 text-gray-500 dark:text-gray-400">Ładowanie prognozy…</div>}
      {isError && <div role="alert" className="text-center py-16 text-red-500">Błąd ładowania danych.</div>}

      {data && (
        <>
          {/* Alert ogólny */}
          {overallStatus !== 'ok' && (
            <div className={`flex items-start gap-3 border rounded-xl p-4 ${STATUS_BG[overallStatus]}`}>
              <AlertTriangle className={`flex-shrink-0 mt-0.5 ${STATUS_COLOR[overallStatus]}`} size={20} />
              <div>
                <p className={`text-sm font-semibold ${STATUS_COLOR[overallStatus]}`}>
                  {overallStatus === 'danger'
                    ? 'Ryzyko braku płynności — prognozowane saldo może spaść poniżej zera w dniu zobowiązania'
                    : 'Uwaga — saldo będzie napięte przy terminie płatności zobowiązań'}
                </p>
                <p className="text-xs text-gray-600 dark:text-gray-400 mt-1">
                  Rozważ odłożenie {fmt(data.monthlyBufferRecommended)} miesięcznie jako bufor podatkowy.
                </p>
              </div>
            </div>
          )}

          {/* KPI Cards */}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
            {[
              {
                label: 'Saldo startowe',
                value: fmt(data.startingBalance),
                sub: 'wprowadzone',
                color: undefined,
              },
              {
                label: 'Oczekiwane wpływy',
                value: fmt(data.totalExpectedInflows),
                sub: `DSO avg ${data.avgDsodays} dni`,
                color: 'text-emerald-600 dark:text-emerald-400',
              },
              {
                label: 'Zobowiązania',
                value: fmt(data.totalObligations),
                sub: `zapłacone: ${fmt(data.totalAlreadyPaid)}`,
                color: 'text-red-500 dark:text-red-400',
              },
              {
                label: 'Saldo za 90 dni',
                value: fmt(projectedBalance),
                sub: projectedBalance >= 0 ? 'prognoza pozytywna' : 'prognoza ujemna',
                color: projectedBalance >= 0
                  ? 'text-emerald-600 dark:text-emerald-400'
                  : 'text-red-600 dark:text-red-400',
              },
            ].map(({ label, value, sub, color }) => (
              <div key={label} className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
                <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">{label}</div>
                <div className={`text-xl font-bold ${color ?? 'text-gray-900 dark:text-gray-100'}`}>{value}</div>
                <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">{sub}</div>
              </div>
            ))}
          </div>

          {/* Kalkulator bufora */}
          <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-xl p-5">
            <div className="flex items-start justify-between flex-wrap gap-4">
              <div>
                <h2 className="text-sm font-semibold text-blue-800 dark:text-blue-300 mb-1 flex items-center gap-2">
                  <TrendingDown size={16} />
                  Kalkulator bufora podatkowego
                </h2>
                <p className="text-xs text-blue-700 dark:text-blue-400 max-w-lg">
                  Aby zawsze mieć środki na ZUS i podatki, odłóż co miesiąc poniższą kwotę na oddzielne konto.
                </p>
              </div>
              <div className="text-right">
                <div className="text-3xl font-bold text-blue-800 dark:text-blue-200">{fmt(data.monthlyBufferRecommended)}</div>
                <div className="text-xs text-blue-600 dark:text-blue-400">/ miesiąc do odłożenia</div>
              </div>
            </div>
            <div className="mt-4 grid grid-cols-3 gap-3 text-center text-sm">
              {[
                { label: 'Śr. ZUS + PIT / mies.', value: fmt(data.avgMonthlyObligations) },
                { label: '% przychodu na podatki', value: data.totalExpectedInflows > 0 ? `${Math.round(data.totalObligations / data.totalExpectedInflows * 100)}%` : '—' },
                { label: 'Rezerwa już zapłacona', value: fmt(data.totalAlreadyPaid) },
              ].map(({ label, value }) => (
                <div key={label} className="bg-blue-100/60 dark:bg-blue-900/30 rounded-lg py-3 px-2">
                  <div className="font-bold text-blue-800 dark:text-blue-200 text-base">{value}</div>
                  <div className="text-xs text-blue-600 dark:text-blue-400 mt-0.5">{label}</div>
                </div>
              ))}
            </div>
          </div>

          {/* Wykres salda */}
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-6">
            <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-1">
              Prognoza salda przez 90 dni
            </h2>
            <p className="text-xs text-gray-500 dark:text-gray-400 mb-4">
              Pionowe linie = terminy płatności (zielone/żółte/czerwone wg ryzyka). Wpływy z faktur co {Math.round(data.avgDsodays)} dni.
            </p>
            <ResponsiveContainer width="100%" height={240}>
              <AreaChart data={chartData} margin={{ top: 8, right: 12, bottom: 0, left: 8 }}>
                <defs>
                  <linearGradient id="balanceGrad" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.3} />
                    <stop offset="95%" stopColor="#3b82f6" stopOpacity={0.02} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                <XAxis dataKey="date" tick={{ fontSize: 10 }} interval={6} />
                <YAxis
                  tickFormatter={fmtShort}
                  tick={{ fontSize: 10 }}
                  width={48}
                />
                <Tooltip content={<BalanceTooltip />} />
                <ReferenceLine y={0} stroke="#ef4444" strokeWidth={1.5} />
                <ObligationMarkers days={data.days} />
                <Area
                  type="monotone"
                  dataKey="balance"
                  stroke="#3b82f6"
                  strokeWidth={2}
                  fill="url(#balanceGrad)"
                  dot={false}
                  activeDot={{ r: 4 }}
                />
              </AreaChart>
            </ResponsiveContainer>
          </div>

          {/* Lista wydarzeń */}
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden">
            <div className="px-4 py-3 border-b border-gray-100 dark:border-gray-800 flex items-center justify-between">
              <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300">
                Nadchodzące zdarzenia
              </h2>
              <div className="flex gap-3 text-xs text-gray-500 dark:text-gray-400">
                <span className="flex items-center gap-1">
                  <span className="w-2 h-2 rounded-full bg-emerald-500" /> OK
                </span>
                <span className="flex items-center gap-1">
                  <span className="w-2 h-2 rounded-full bg-amber-400" /> Napięte
                </span>
                <span className="flex items-center gap-1">
                  <span className="w-2 h-2 rounded-full bg-red-500" /> Ryzyko
                </span>
              </div>
            </div>
            <div className="divide-y divide-gray-50 dark:divide-gray-800">
              {data.events.length === 0 ? (
                <div className="px-4 py-8 text-center text-sm text-gray-500 dark:text-gray-400">
                  Brak zdarzeń w ciągu najbliższych 90 dni.
                </div>
              ) : (
                <div className="p-4 space-y-2">
                  {data.events
                    .filter(e => e.status !== 'paid')
                    .map((event, i) => (
                      <EventRow key={i} event={event} />
                    ))}
                  {data.events.some(e => e.status === 'paid') && (
                    <>
                      <div className="pt-2 pb-1">
                        <div className="flex items-center gap-2">
                          <CheckCircle size={14} className="text-gray-400" />
                          <span className="text-xs text-gray-500 dark:text-gray-400 font-medium">Już zapłacone</span>
                        </div>
                      </div>
                      {data.events
                        .filter(e => e.status === 'paid')
                        .map((event, i) => (
                          <EventRow key={`paid-${i}`} event={event} />
                        ))}
                    </>
                  )}
                </div>
              )}
            </div>
          </div>
        </>
      )}
    </div>
  )
}
