import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  ComposedChart, Bar, Line, XAxis, YAxis, Tooltip, Legend,
  ResponsiveContainer, CartesianGrid,
  PieChart, Pie, Cell,
} from 'recharts'
import { Globe } from 'lucide-react'
import { getCurrencyExposure } from '../api/currencyExposure'
import type { CurrencyBreakdown } from '../types/currencyExposure'

// ─── Formatowanie ──────────────────────────────────────────────────────────

const fmt = (v: number) => v.toLocaleString('pl-PL', { maximumFractionDigits: 0 }) + ' PLN'
const fmtK = (v: number) => {
  if (Math.abs(v) >= 1000) return (v / 1000).toLocaleString('pl-PL', { maximumFractionDigits: 1 }) + 'k'
  return v.toLocaleString('pl-PL', { maximumFractionDigits: 0 })
}
const fmtRate = (v: number | null | undefined) =>
  v != null ? v.toLocaleString('pl-PL', { minimumFractionDigits: 4, maximumFractionDigits: 4 }) : '—'
const fmtForeign = (v: number, cur: string) =>
  v.toLocaleString('pl-PL', { maximumFractionDigits: 2 }) + ' ' + cur

// ─── Kolory walut ──────────────────────────────────────────────────────────

const CURRENCY_COLORS: Record<string, string> = {
  PLN: '#6b7280',
  EUR: '#3b82f6',
  USD: '#10b981',
  GBP: '#8b5cf6',
  CHF: '#f59e0b',
}

const SENSITIVITY_COLOR = (pct: number) =>
  pct < 0 ? '#ef4444' : pct > 0 ? '#10b981' : '#6b7280'

// ─── Pie label ─────────────────────────────────────────────────────────────

interface PieLabelProps {
  cx: number; cy: number; midAngle: number; innerRadius: number
  outerRadius: number; percent: number; name: string
}

function PieLabel({ cx, cy, midAngle, innerRadius, outerRadius, percent, name }: PieLabelProps) {
  if (percent < 0.04) return null
  const RADIAN = Math.PI / 180
  const r = innerRadius + (outerRadius - innerRadius) * 0.5
  const x = cx + r * Math.cos(-midAngle * RADIAN)
  const y = cy + r * Math.sin(-midAngle * RADIAN)
  return (
    <text x={x} y={y} fill="#fff" textAnchor="middle" dominantBaseline="central" fontSize={12} fontWeight={600}>
      {name} {(percent * 100).toFixed(0)}%
    </text>
  )
}

// ─── Breakdown card ────────────────────────────────────────────────────────

function BreakdownCard({ b }: { b: CurrencyBreakdown }) {
  const isForeign = b.currency !== 'PLN'
  const color = CURRENCY_COLORS[b.currency] ?? '#6b7280'
  return (
    <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
      <div className="flex items-center gap-2 mb-3">
        <span className="w-3 h-3 rounded-full flex-shrink-0" style={{ backgroundColor: color }} />
        <span className="font-bold text-lg text-gray-900 dark:text-gray-100">{b.currency}</span>
        <span className="ml-auto text-sm font-semibold text-gray-700 dark:text-gray-300">{b.sharePercent}%</span>
      </div>
      <div className="text-xl font-bold text-gray-900 dark:text-gray-100 mb-1">{fmt(b.totalNetPln)}</div>
      {isForeign && (
        <div className="text-sm text-gray-500 dark:text-gray-400 mb-2">
          {fmtForeign(b.totalNetForeign, b.currency)}
        </div>
      )}
      <div className="text-xs text-gray-500 dark:text-gray-400">{b.invoiceCount} {b.invoiceCount === 1 ? 'faktura' : 'faktur'}</div>
      {isForeign && b.avgExchangeRate > 0 && (
        <div className="mt-2 pt-2 border-t border-gray-100 dark:border-gray-800 text-xs space-y-1">
          <div className="flex justify-between text-gray-500 dark:text-gray-400">
            <span>Kurs avg</span>
            <span className="font-mono">{fmtRate(b.avgExchangeRate)}</span>
          </div>
          <div className="flex justify-between text-gray-500 dark:text-gray-400">
            <span>Min / Max</span>
            <span className="font-mono">{fmtRate(b.minExchangeRate)} / {fmtRate(b.maxExchangeRate)}</span>
          </div>
          <div className="flex justify-between text-gray-500 dark:text-gray-400">
            <span>Zmienność kursu</span>
            <span className={`font-mono ${b.maxExchangeRate - b.minExchangeRate > 0.2 ? 'text-amber-500' : 'text-gray-400'}`}>
              ±{fmtRate(b.maxExchangeRate - b.minExchangeRate)}
            </span>
          </div>
        </div>
      )}
    </div>
  )
}

// ─── Strona ────────────────────────────────────────────────────────────────

export function CurrencyExposurePage() {
  const currentYear = new Date().getFullYear()
  const [year, setYear] = useState(currentYear)
  const [customChange, setCustomChange] = useState(0)

  const { data, isLoading, isError } = useQuery({
    queryKey: ['currency-exposure', year],
    queryFn: () => getCurrencyExposure(year),
  })

  // Dane do wykresu miesięcznego (skrócone nazwy)
  const monthlyChartData = (data?.monthlyTrend ?? []).map(m => ({
    name: m.monthName.slice(0, 3),
    PLN: m.plnRevenue,
    EUR: m.eurRevenuePln,
    USD: m.usdRevenuePln,
    GBP: m.gbpRevenuePln,
    CHF: m.chfRevenuePln,
    'Kurs EUR': m.eurRate,
  }))

  // Dane do pie chart
  const pieData = (data?.breakdown ?? []).map(b => ({
    name: b.currency,
    value: b.totalNetPln,
  }))

  // Symulator własny (klient-side)
  const customEurImpact = data ? Math.round(
    (data.breakdown.find(b => b.currency === 'EUR')?.totalNetPln ?? 0) * customChange / 100
  ) : 0
  const customUsdImpact = data ? Math.round(
    (data.breakdown.find(b => b.currency === 'USD')?.totalNetPln ?? 0) * customChange / 100
  ) : 0
  const customGbpImpact = data ? Math.round(
    (data.breakdown.find(b => b.currency === 'GBP')?.totalNetPln ?? 0) * customChange / 100
  ) : 0
  const customChfImpact = data ? Math.round(
    (data.breakdown.find(b => b.currency === 'CHF')?.totalNetPln ?? 0) * customChange / 100
  ) : 0
  const customTotalImpact = customEurImpact + customUsdImpact + customGbpImpact + customChfImpact
  const customAdjusted = data ? data.totalRevenuePln + customTotalImpact : 0

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-4">
        <div className="flex items-center gap-3">
          <Globe className="text-blue-600" size={26} />
          <div>
            <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Ekspozycja walutowa</h1>
            {data && (
              <p className="text-sm text-gray-500 dark:text-gray-400">
                Przychód walutowy: {data.foreignSharePercent}% całości
              </p>
            )}
          </div>
        </div>
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
      </div>

      {isLoading && <div className="text-center py-16 text-gray-500 dark:text-gray-400">Ładowanie danych…</div>}
      {isError && <div role="alert" className="text-center py-16 text-red-500">Błąd ładowania danych.</div>}

      {data && data.breakdown.length === 0 && (
        <div className="text-center py-16 text-gray-500 dark:text-gray-400">Brak faktur w tym roku.</div>
      )}

      {data && data.breakdown.length > 0 && (
        <>
          {/* KPI cards */}
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
            {[
              { label: 'Przychód ogółem', value: fmt(data.totalRevenuePln), sub: `${year}` },
              { label: 'Przychód walutowy', value: fmt(data.foreignRevenuePln), sub: `${data.foreignSharePercent}% całości`, color: 'text-blue-600 dark:text-blue-400' },
              { label: 'Przychód PLN', value: fmt(data.plnOnlyRevenuePln), sub: `${(100 - data.foreignSharePercent).toFixed(1)}% całości` },
            ].map(({ label, value, sub, color }) => (
              <div key={label} className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
                <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">{label}</div>
                <div className={`text-xl font-bold ${color ?? 'text-gray-900 dark:text-gray-100'}`}>{value}</div>
                <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">{sub}</div>
              </div>
            ))}
          </div>

          {/* Breakdown per waluta + Pie */}
          <div className="grid grid-cols-1 lg:grid-cols-5 gap-6">
            {/* Karty walut */}
            <div className="lg:col-span-3 grid grid-cols-2 sm:grid-cols-3 gap-3">
              {data.breakdown.map(b => <BreakdownCard key={b.currency} b={b} />)}
            </div>

            {/* Pie chart */}
            <div className="lg:col-span-2 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-5">
              <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">Udział walut w przychodzie</h2>
              <ResponsiveContainer width="100%" height={220}>
                <PieChart>
                  <Pie
                    data={pieData}
                    cx="50%"
                    cy="50%"
                    outerRadius={90}
                    dataKey="value"
                    labelLine={false}
                    label={PieLabel as any}
                  >
                    {pieData.map((entry) => (
                      <Cell key={entry.name} fill={CURRENCY_COLORS[entry.name] ?? '#6b7280'} />
                    ))}
                  </Pie>
                  <Tooltip formatter={(v) => fmt(Number(v))} />
                </PieChart>
              </ResponsiveContainer>
              <div className="flex flex-wrap gap-3 justify-center mt-1">
                {data.breakdown.map(b => (
                  <div key={b.currency} className="flex items-center gap-1.5 text-xs text-gray-600 dark:text-gray-300">
                    <span className="w-2.5 h-2.5 rounded-full" style={{ backgroundColor: CURRENCY_COLORS[b.currency] ?? '#6b7280' }} />
                    {b.currency}
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Wykres miesięczny + kursy NBP */}
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-6">
            <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-1">
              Miesięczny przychód wg waluty vs. kurs EUR/PLN (NBP)
            </h2>
            <p className="text-xs text-gray-500 dark:text-gray-400 mb-4">
              Słupki = przychód w PLN per waluta; linia = średni kurs EUR z tabeli NBP (prawa oś)
            </p>
            <ResponsiveContainer width="100%" height={260}>
              <ComposedChart data={monthlyChartData} margin={{ top: 4, right: 40, bottom: 0, left: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                <YAxis
                  yAxisId="left"
                  tickFormatter={v => `${fmtK(v)}`}
                  tick={{ fontSize: 10 }}
                  width={44}
                />
                <YAxis
                  yAxisId="right"
                  orientation="right"
                  domain={['auto', 'auto']}
                  tickFormatter={v => v.toFixed(2)}
                  tick={{ fontSize: 10 }}
                  width={44}
                  label={{ value: 'EUR/PLN', angle: 90, position: 'insideRight', fontSize: 10, offset: 8 }}
                />
                <Tooltip
                  formatter={(v, name) =>
                    name === 'Kurs EUR'
                      ? [Number(v).toFixed(4), 'Kurs EUR/PLN']
                      : [fmt(Number(v)), String(name)]
                  }
                />
                <Legend wrapperStyle={{ fontSize: 11 }} />
                {['PLN', 'EUR', 'USD', 'GBP', 'CHF']
                  .filter(cur => data.breakdown.some(b => b.currency === cur))
                  .map(cur => (
                    <Bar
                      key={cur}
                      yAxisId="left"
                      dataKey={cur}
                      stackId="a"
                      fill={CURRENCY_COLORS[cur]}
                      radius={cur === 'CHF' || (data.breakdown[0]?.currency === cur && data.breakdown.length === 1) ? [4, 4, 0, 0] : [0, 0, 0, 0]}
                    />
                  ))}
                {data.monthlyTrend.some(m => m.eurRate != null) && (
                  <Line
                    yAxisId="right"
                    dataKey="Kurs EUR"
                    stroke="#f59e0b"
                    strokeWidth={2}
                    dot={false}
                    connectNulls
                  />
                )}
              </ComposedChart>
            </ResponsiveContainer>
          </div>

          {/* Analiza wrażliwości */}
          {data.foreignRevenuePln > 0 && (
            <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden">
              <div className="px-5 py-4 border-b border-gray-100 dark:border-gray-800">
                <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300">
                  Analiza wrażliwości — wpływ zmiany kursów na roczny przychód PLN
                </h2>
                <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                  Symulacja przy założeniu równoczesnej zmiany wszystkich kursów walutowych o ten sam %.
                </p>
              </div>

              {/* Tabela scenariuszy */}
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="text-xs text-gray-500 dark:text-gray-400 border-b border-gray-100 dark:border-gray-800">
                      <th className="px-4 py-2 text-left">Zmiana kursów</th>
                      {data.breakdown.filter(b => b.currency !== 'PLN').map(b => (
                        <th key={b.currency} className="px-4 py-2 text-right">{b.currency} wpływ</th>
                      ))}
                      <th className="px-4 py-2 text-right">Wpływ łączny</th>
                      <th className="px-4 py-2 text-right">Przychód po korekcie</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.sensitivity.map(row => (
                      <tr
                        key={row.changePercent}
                        className={`border-b border-gray-50 dark:border-gray-800 ${row.changePercent === 0 ? 'bg-gray-50 dark:bg-gray-800/50 font-semibold' : ''}`}
                      >
                        <td className="px-4 py-2.5">
                          <span
                            className="font-semibold"
                            style={{ color: SENSITIVITY_COLOR(row.changePercent) }}
                          >
                            {row.changePercent > 0 ? '+' : ''}{row.changePercent}%
                          </span>
                          {row.changePercent === 0 && <span className="ml-2 text-xs text-gray-500 dark:text-gray-400">(bieżący)</span>}
                        </td>
                        {data.breakdown.filter(b => b.currency !== 'PLN').map(b => {
                          const impact = b.currency === 'EUR' ? row.eurImpact
                            : b.currency === 'USD' ? row.usdImpact
                            : b.currency === 'GBP' ? row.gbpImpact
                            : row.chfImpact
                          return (
                            <td key={b.currency} className="px-4 py-2.5 text-right font-mono text-xs"
                              style={{ color: impact === 0 ? undefined : SENSITIVITY_COLOR(impact) }}>
                              {impact === 0 ? '—' : `${impact > 0 ? '+' : ''}${fmt(impact)}`}
                            </td>
                          )
                        })}
                        <td className="px-4 py-2.5 text-right font-semibold"
                          style={{ color: SENSITIVITY_COLOR(row.totalImpact) }}>
                          {row.totalImpact === 0 ? '—' : `${row.totalImpact > 0 ? '+' : ''}${fmt(row.totalImpact)}`}
                        </td>
                        <td className="px-4 py-2.5 text-right text-gray-900 dark:text-gray-100">
                          {fmt(row.adjustedTotalRevenue)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Symulator niestandardowy */}
              <div className="px-5 py-5 border-t border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/30">
                <h3 className="text-xs font-semibold text-gray-600 dark:text-gray-400 mb-3">
                  Symulator własny
                </h3>
                <div className="flex items-center gap-4 flex-wrap">
                  <div className="flex items-center gap-2">
                    <label className="text-sm text-gray-600 dark:text-gray-300 whitespace-nowrap">
                      Zmiana kursów:
                    </label>
                    <input
                      type="range"
                      aria-label="Zmiana kursów (%)"
                      min={-50}
                      max={50}
                      step={1}
                      value={customChange}
                      onChange={e => setCustomChange(Number(e.target.value))}
                      className="w-40 accent-blue-500"
                    />
                    <span className="text-sm font-bold w-12 text-center"
                      style={{ color: SENSITIVITY_COLOR(customChange) }}>
                      {customChange > 0 ? '+' : ''}{customChange}%
                    </span>
                  </div>
                  <div className="flex gap-6 text-sm flex-wrap">
                    <div>
                      <span className="text-gray-500 dark:text-gray-400">Wpływ łączny: </span>
                      <span className="font-bold" style={{ color: SENSITIVITY_COLOR(customTotalImpact) }}>
                        {customTotalImpact >= 0 ? '+' : ''}{fmt(customTotalImpact)}
                      </span>
                    </div>
                    <div>
                      <span className="text-gray-500 dark:text-gray-400">Przychód po korekcie: </span>
                      <span className="font-bold text-gray-900 dark:text-gray-100">{fmt(customAdjusted)}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Tabela kursów miesięcznych NBP */}
          {data.monthlyTrend.some(m => m.eurRate != null || m.usdRate != null) && (
            <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden">
              <div className="px-4 py-3 border-b border-gray-100 dark:border-gray-800">
                <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300">
                  Średnie kursy NBP per miesiąc
                </h2>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="text-xs text-gray-500 dark:text-gray-400 border-b border-gray-100 dark:border-gray-800">
                      <th className="px-4 py-2 text-left">Miesiąc</th>
                      <th className="px-4 py-2 text-right">EUR/PLN</th>
                      <th className="px-4 py-2 text-right">USD/PLN</th>
                      <th className="px-4 py-2 text-right">GBP/PLN</th>
                      <th className="px-4 py-2 text-right">Przychód ogółem</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.monthlyTrend.filter(m => m.totalRevenuePln > 0 || m.eurRate != null).map(m => (
                      <tr key={m.month} className="border-b border-gray-50 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-800/50">
                        <td className="px-4 py-2 font-medium text-gray-800 dark:text-gray-200">{m.monthName}</td>
                        <td className="px-4 py-2 text-right font-mono text-xs text-gray-600 dark:text-gray-300">{fmtRate(m.eurRate)}</td>
                        <td className="px-4 py-2 text-right font-mono text-xs text-gray-600 dark:text-gray-300">{fmtRate(m.usdRate)}</td>
                        <td className="px-4 py-2 text-right font-mono text-xs text-gray-600 dark:text-gray-300">{fmtRate(m.gbpRate)}</td>
                        <td className="px-4 py-2 text-right text-gray-700 dark:text-gray-300">
                          {m.totalRevenuePln > 0 ? fmt(m.totalRevenuePln) : '—'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}
