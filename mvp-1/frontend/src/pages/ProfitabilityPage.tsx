import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  ScatterChart, Scatter, XAxis, YAxis, ZAxis, Tooltip,
  ResponsiveContainer, CartesianGrid, Cell,
} from 'recharts'
import { AlertTriangle, ChevronDown, ChevronRight, TrendingUp } from 'lucide-react'
import { getClientProfitability } from '../api/profitability'
import type { ClientProfitability } from '../types/profitability'

const fmt = (v: number) => v.toLocaleString('pl-PL', { maximumFractionDigits: 0 }) + ' PLN'
const fmtRate = (v: number) => v.toLocaleString('pl-PL', { maximumFractionDigits: 0 }) + ' PLN/h'
const fmtH = (v: number) => v.toLocaleString('pl-PL', { maximumFractionDigits: 1 }) + ' h'

const COLORS = [
  '#3b82f6', '#10b981', '#f59e0b', '#ef4444',
  '#8b5cf6', '#06b6d4', '#f97316', '#84cc16',
]

const MONTHS = [
  '', 'Styczeń', 'Luty', 'Marzec', 'Kwiecień', 'Maj', 'Czerwiec',
  'Lipiec', 'Sierpień', 'Wrzesień', 'Październik', 'Listopad', 'Grudzień',
]

function ShareBar({ pct, color }: { pct: number; color: string }) {
  return (
    <div className="flex items-center gap-2">
      <div className="w-24 h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
        <div className="h-2 rounded-full" style={{ width: `${pct}%`, backgroundColor: color }} />
      </div>
      <span className="text-xs text-gray-500 dark:text-gray-400">{pct.toFixed(1)}%</span>
    </div>
  )
}

function ClientRow({ client, color, minRate }: { client: ClientProfitability; color: string; minRate: number }) {
  const [open, setOpen] = useState(false)
  const needed = client.billableHours > 0
    ? Math.ceil((minRate * client.billableHours - client.revenuePln) / client.billableHours + client.effectiveRatePlnPerHour)
    : null
  const rateOk = client.effectiveRatePlnPerHour >= minRate || minRate === 0

  return (
    <>
      <tr
        className="border-b border-gray-100 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-800/50 cursor-pointer"
        onClick={() => setOpen(o => !o)}
      >
        <td className="px-4 py-3">
          <div className="flex items-center gap-2">
            <span className="w-3 h-3 rounded-full flex-shrink-0" style={{ backgroundColor: color }} />
            <span className="font-medium text-gray-900 dark:text-gray-100 text-sm">{client.clientName}</span>
            {client.projects.length > 0 && (
              open ? <ChevronDown size={14} className="text-gray-400" /> : <ChevronRight size={14} className="text-gray-400" />
            )}
          </div>
          <div className="text-xs text-gray-500 dark:text-gray-400 ml-5">{client.clientNip}</div>
        </td>
        <td className="px-4 py-3 text-right text-sm text-gray-900 dark:text-gray-100">{fmt(client.revenuePln)}</td>
        <td className="px-4 py-3 text-right text-sm text-gray-600 dark:text-gray-300">{fmtH(client.billableHours)}</td>
        <td className="px-4 py-3 text-right text-sm">
          <span className={`font-semibold ${rateOk ? 'text-emerald-600 dark:text-emerald-400' : 'text-red-500 dark:text-red-400'}`}>
            {fmtRate(client.effectiveRatePlnPerHour)}
          </span>
          {!rateOk && needed !== null && (
            <div className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">min. {fmtRate(needed)} potrzebne</div>
          )}
        </td>
        <td className="px-4 py-3">
          <ShareBar pct={client.revenueSharePercent} color={color} />
        </td>
        <td className="px-4 py-3 text-center text-sm text-gray-500 dark:text-gray-400">{client.invoiceCount}</td>
      </tr>
      {open && client.projects.map(p => (
        <tr key={p.projectId} className="bg-gray-50 dark:bg-gray-900/50 border-b border-gray-100 dark:border-gray-800">
          <td className="px-4 py-2 pl-10">
            <span className="text-sm text-gray-600 dark:text-gray-300">↳ {p.projectName}</span>
          </td>
          <td className="px-4 py-2 text-right text-xs text-gray-400">—</td>
          <td className="px-4 py-2 text-right text-xs text-gray-500 dark:text-gray-400">{fmtH(p.billableHours)}</td>
          <td className="px-4 py-2 text-right text-xs text-gray-500 dark:text-gray-400">
            {p.billableHours > 0 ? fmtRate(p.effectiveRatePlnPerHour) : '—'}
          </td>
          <td colSpan={2} />
        </tr>
      ))}
    </>
  )
}

interface BubbleTooltipProps {
  active?: boolean
  payload?: Array<{ payload: { name: string; revenue: number; hours: number; rate: number } }>
}

function BubbleTooltip({ active, payload }: BubbleTooltipProps) {
  if (!active || !payload?.length) return null
  const d = payload[0].payload
  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-3 shadow-lg text-sm">
      <div className="font-semibold text-gray-900 dark:text-gray-100 mb-1">{d.name}</div>
      <div className="text-gray-600 dark:text-gray-300">Przychód: {fmt(d.revenue)}</div>
      <div className="text-gray-600 dark:text-gray-300">Godziny: {fmtH(d.hours)}</div>
      <div className="text-emerald-600 dark:text-emerald-400 font-medium">Stawka: {fmtRate(d.rate)}</div>
    </div>
  )
}

export function ProfitabilityPage() {
  const currentYear = new Date().getFullYear()
  const [year, setYear] = useState(currentYear)
  const [month, setMonth] = useState<number | undefined>(undefined)
  const [minRate, setMinRate] = useState(150)

  const { data, isLoading, isError } = useQuery({
    queryKey: ['profitability', year, month],
    queryFn: () => getClientProfitability(year, month),
  })

  const bubbleData = (data?.clients ?? []).map((c, i) => ({
    name: c.clientName,
    hours: c.billableHours,
    revenue: c.revenuePln,
    rate: c.effectiveRatePlnPerHour,
    z: Math.max(c.effectiveRatePlnPerHour, 10),
    color: COLORS[i % COLORS.length],
  }))

  return (
    <div className="p-6 max-w-6xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-4">
        <div className="flex items-center gap-3">
          <TrendingUp className="text-blue-600" size={26} />
          <div>
            <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Rentowność klientów</h1>
            {data && (
              <p className="text-sm text-gray-500 dark:text-gray-400">{data.period}</p>
            )}
          </div>
        </div>

        {/* Filtry */}
        <div className="flex items-center gap-3 flex-wrap">
          <select
            aria-label="Rok"
            value={year}
            onChange={e => setYear(Number(e.target.value))}
            className="border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
          >
            {[currentYear - 1, currentYear, currentYear + 1].map(y => (
              <option key={y} value={y}>{y}</option>
            ))}
          </select>
          <select
            aria-label="Miesiąc"
            value={month ?? ''}
            onChange={e => setMonth(e.target.value ? Number(e.target.value) : undefined)}
            className="border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
          >
            <option value="">Cały rok</option>
            {MONTHS.slice(1).map((m, i) => (
              <option key={i + 1} value={i + 1}>{m}</option>
            ))}
          </select>
          <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-300">
            <label htmlFor="minRate">Min. stawka:</label>
            <input
              id="minRate"
              type="number"
              min={0}
              step={10}
              value={minRate}
              onChange={e => setMinRate(Number(e.target.value))}
              className="w-24 border border-gray-300 dark:border-gray-600 rounded-lg px-2 py-2 text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
            />
            <span>PLN/h</span>
          </div>
        </div>
      </div>

      {/* Alert koncentracji */}
      {data?.hasConcentrationRisk && (
        <div className="flex items-start gap-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-300 dark:border-amber-700 rounded-xl p-4">
          <AlertTriangle className="text-amber-500 flex-shrink-0 mt-0.5" size={20} />
          <p className="text-sm text-amber-800 dark:text-amber-200">{data.concentrationWarning}</p>
        </div>
      )}

      {/* KPI cards */}
      {data && (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
          {[
            { label: 'Przychód ogółem', value: fmt(data.totalRevenuePln), sub: data.period },
            { label: 'Godziny billable', value: fmtH(data.totalBillableHours), sub: 'zarejestrowane' },
            { label: 'Efektywna stawka', value: fmtRate(data.overallEffectiveRate), sub: 'średnia ważona' },
            { label: 'Liczba klientów', value: String(data.clients.length), sub: 'aktywnych w okresie' },
          ].map(({ label, value, sub }) => (
            <div key={label} className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
              <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">{label}</div>
              <div className="text-xl font-bold text-gray-900 dark:text-gray-100">{value}</div>
              <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">{sub}</div>
            </div>
          ))}
        </div>
      )}

      {/* Loading / Error */}
      {isLoading && (
        <div className="text-center py-16 text-gray-500 dark:text-gray-400">Ładowanie danych…</div>
      )}
      {isError && (
        <div className="text-center py-16 text-red-500">Błąd ładowania danych.</div>
      )}

      {data && data.clients.length === 0 && (
        <div className="text-center py-16 text-gray-500 dark:text-gray-400">
          Brak faktur ani wpisów czasowych w tym okresie.
        </div>
      )}

      {data && data.clients.length > 0 && (
        <>
          {/* Bubble chart */}
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-6">
            <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">
              Mapa rentowności — oś X: godziny, oś Y: przychód, rozmiar: stawka/h
            </h2>
            <ResponsiveContainer width="100%" height={260}>
              <ScatterChart margin={{ top: 10, right: 30, bottom: 10, left: 10 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                <XAxis
                  dataKey="hours"
                  name="Godziny"
                  type="number"
                  tickFormatter={v => `${v}h`}
                  tick={{ fontSize: 11 }}
                  label={{ value: 'Godziny billable', position: 'insideBottom', offset: -4, fontSize: 11 }}
                />
                <YAxis
                  dataKey="revenue"
                  name="Przychód"
                  type="number"
                  tickFormatter={v => `${(v / 1000).toFixed(0)}k`}
                  tick={{ fontSize: 11 }}
                  width={52}
                />
                <ZAxis dataKey="z" range={[200, 1200]} />
                <Tooltip content={<BubbleTooltip />} />
                <Scatter data={bubbleData} fill="#3b82f6">
                  {bubbleData.map((entry, i) => (
                    <Cell key={i} fill={entry.color} fillOpacity={0.8} />
                  ))}
                </Scatter>
              </ScatterChart>
            </ResponsiveContainer>
            {/* Legenda */}
            <div className="flex flex-wrap gap-3 mt-3 justify-center">
              {bubbleData.map((d, i) => (
                <div key={i} className="flex items-center gap-1.5 text-xs text-gray-600 dark:text-gray-300">
                  <span className="w-2.5 h-2.5 rounded-full" style={{ backgroundColor: d.color }} />
                  {d.name}
                </div>
              ))}
            </div>
          </div>

          {/* Tabela */}
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden">
            <div className="px-4 py-3 border-b border-gray-100 dark:border-gray-800">
              <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300">
                Szczegóły per klient
                <span className="ml-2 text-xs font-normal text-gray-500 dark:text-gray-400">
                  (kliknij wiersz, aby rozwinąć projekty)
                </span>
              </h2>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="text-xs text-gray-500 dark:text-gray-400 border-b border-gray-100 dark:border-gray-800">
                    <th className="px-4 py-2 text-left">Klient</th>
                    <th className="px-4 py-2 text-right">Przychód</th>
                    <th className="px-4 py-2 text-right">Godziny</th>
                    <th className="px-4 py-2 text-right">Stawka/h</th>
                    <th className="px-4 py-2 text-left">Udział</th>
                    <th className="px-4 py-2 text-center">Faktur</th>
                  </tr>
                </thead>
                <tbody>
                  {data.clients.map((client, i) => (
                    <ClientRow
                      key={client.clientId}
                      client={client}
                      color={COLORS[i % COLORS.length]}
                      minRate={minRate}
                    />
                  ))}
                </tbody>
                <tfoot>
                  <tr className="bg-gray-50 dark:bg-gray-800/50 text-sm font-semibold border-t-2 border-gray-200 dark:border-gray-700">
                    <td className="px-4 py-3 text-gray-700 dark:text-gray-300">Suma</td>
                    <td className="px-4 py-3 text-right text-gray-900 dark:text-gray-100">{fmt(data.totalRevenuePln)}</td>
                    <td className="px-4 py-3 text-right text-gray-900 dark:text-gray-100">{fmtH(data.totalBillableHours)}</td>
                    <td className="px-4 py-3 text-right text-gray-900 dark:text-gray-100">{fmtRate(data.overallEffectiveRate)}</td>
                    <td className="px-4 py-3 text-xs text-gray-500 dark:text-gray-400">100%</td>
                    <td className="px-4 py-3 text-center text-gray-900 dark:text-gray-100">
                      {data.clients.reduce((s, c) => s + c.invoiceCount, 0)}
                    </td>
                  </tr>
                </tfoot>
              </table>
            </div>
          </div>

          {/* Kalkulator minimalnej stawki */}
          {minRate > 0 && (
            <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-xl p-4">
              <h2 className="text-sm font-semibold text-blue-800 dark:text-blue-300 mb-3">
                Kalkulator: ile potrzebuję, żeby osiągnąć {minRate} PLN/h?
              </h2>
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                {data.clients.filter(c => c.billableHours > 0).map((c, i) => {
                  const targetRevenue = minRate * c.billableHours
                  const gap = targetRevenue - c.revenuePln
                  const ok = gap <= 0
                  return (
                    <div
                      key={c.clientId}
                      className={`rounded-lg p-3 text-sm ${ok
                        ? 'bg-emerald-50 dark:bg-emerald-900/20 border border-emerald-200 dark:border-emerald-800'
                        : 'bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700'}`}
                    >
                      <div className="flex items-center gap-2 mb-1">
                        <span className="w-2.5 h-2.5 rounded-full" style={{ backgroundColor: COLORS[i % COLORS.length] }} />
                        <span className="font-medium text-gray-800 dark:text-gray-200 truncate">{c.clientName}</span>
                      </div>
                      {ok ? (
                        <div className="text-emerald-700 dark:text-emerald-400">
                          ✓ Cel osiągnięty ({fmtRate(c.effectiveRatePlnPerHour)})
                        </div>
                      ) : (
                        <div className="text-gray-600 dark:text-gray-300">
                          Brakuje <span className="font-semibold text-red-600 dark:text-red-400">{fmt(gap)}</span>
                          <div className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                            Potrzeba {fmt(targetRevenue)} za {fmtH(c.billableHours)}
                          </div>
                        </div>
                      )}
                    </div>
                  )
                })}
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}
