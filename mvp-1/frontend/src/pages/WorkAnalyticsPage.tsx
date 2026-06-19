import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  ComposedChart, Bar, XAxis, YAxis, Tooltip, Legend,
  ResponsiveContainer, CartesianGrid,
} from 'recharts'
import { AlertTriangle, Clock } from 'lucide-react'
import { getWorkAnalytics } from '../api/workAnalytics'
import type { DayWork } from '../types/workAnalytics'

const fmtH = (v: number) => v.toLocaleString('pl-PL', { maximumFractionDigits: 1 }) + ' h'

// ─── Heatmap ────────────────────────────────────────────────────────────────

function hourColor(hours: number): string {
  if (hours === 0) return '#e2e8f0'
  if (hours < 2) return '#bfdbfe'
  if (hours < 4) return '#93c5fd'
  if (hours < 6) return '#60a5fa'
  return '#3b82f6'
}

function WorkHeatmap({ days, year }: { days: DayWork[]; year: number }) {
  const dayMap = new Map(days.map(d => [d.date, d]))

  // Pierwszy dzień roku → cofnij do poniedziałku
  const jan1 = new Date(year, 0, 1)
  const offset = (jan1.getDay() + 6) % 7 // 0=Mon … 6=Sun
  const firstCell = new Date(jan1)
  firstCell.setDate(jan1.getDate() - offset)

  // Buduj tygodnie (kolumny) × 7 dni (wiersze)
  const weeks: Date[][] = []
  const cursor = new Date(firstCell)
  while (cursor.getFullYear() <= year) {
    const week: Date[] = []
    for (let d = 0; d < 7; d++) {
      week.push(new Date(cursor))
      cursor.setDate(cursor.getDate() + 1)
    }
    weeks.push(week)
    if (cursor.getFullYear() > year) break
  }

  const DAY_LABELS = ['Pn', 'Wt', 'Śr', 'Cz', 'Pt', 'So', 'Nd']
  const MONTH_NAMES = ['Sty', 'Lut', 'Mar', 'Kwi', 'Maj', 'Cze',
    'Lip', 'Sie', 'Wrz', 'Paź', 'Lis', 'Gru']

  return (
    <div className="overflow-x-auto pb-2">
      <div className="flex gap-0.5">
        {/* Etykiety wierszy (dni tygodnia) */}
        <div className="flex flex-col gap-0.5 mr-1 justify-start">
          <div className="h-4" /> {/* odstęp na miesiące */}
          {DAY_LABELS.map(l => (
            <div key={l} className="h-3 text-[9px] text-gray-500 dark:text-gray-400 leading-3 w-5 text-right pr-1">{l}</div>
          ))}
        </div>

        {/* Kolumny tygodniowe */}
        {weeks.map((week, wi) => {
          // Sprawdź czy ten tydzień zaczyna nowy miesiąc
          const firstInYear = week.find(d => d.getFullYear() === year)
          const showMonth = firstInYear?.getDate()! <= 7 && wi > 0
            ? MONTH_NAMES[firstInYear!.getMonth()]
            : (wi === 0 && firstInYear ? MONTH_NAMES[firstInYear.getMonth()] : '')

          return (
            <div key={wi} className="flex flex-col gap-0.5">
              <div className="h-4 text-[9px] text-gray-500 dark:text-gray-400 whitespace-nowrap leading-4">
                {showMonth}
              </div>
              {week.map((day, di) => {
                const inYear = day.getFullYear() === year
                const key = inYear ? day.toISOString().slice(0, 10) : ''
                const data = dayMap.get(key)
                const hours = data?.hours ?? 0
                const isWeekend = di >= 5
                return (
                  <div
                    key={di}
                    className="w-3 h-3 rounded-sm"
                    style={{
                      backgroundColor: inYear
                        ? (isWeekend && hours === 0 ? '#f1f5f9' : hourColor(hours))
                        : 'transparent',
                      opacity: inYear ? 1 : 0,
                    }}
                    title={inYear ? `${key}: ${fmtH(hours)}` : ''}
                  />
                )
              })}
            </div>
          )
        })}
      </div>

      {/* Legenda */}
      <div className="flex items-center gap-2 mt-3 text-xs text-gray-500 dark:text-gray-400">
        <span>Mniej</span>
        {[0, 1, 3, 5, 7].map(h => (
          <div key={h} className="w-3 h-3 rounded-sm" style={{ backgroundColor: hourColor(h) }} />
        ))}
        <span>Więcej</span>
      </div>
    </div>
  )
}

// ─── Strona ──────────────────────────────────────────────────────────────────

export function WorkAnalyticsPage() {
  const currentYear = new Date().getFullYear()
  const [year, setYear] = useState(currentYear)

  const { data, isLoading, isError } = useQuery({
    queryKey: ['work-analytics', year],
    queryFn: () => getWorkAnalytics(year),
  })

  const s = data?.summary

  // Wykres miesięczny
  const chartData = (data?.months ?? []).map(m => ({
    name: m.monthName.slice(0, 3),
    'Zafakturowane': m.invoicedHours,
    'Niezafakturowane': m.pendingHours,
  }))

  // Wskaźnik urlopowy
  const standardVacation = 26
  const freeDays = s?.freeDays ?? 0
  const vacationStatus =
    freeDays >= standardVacation ? 'ok'
    : freeDays >= Math.round(standardVacation * 0.5) ? 'warning'
    : 'low'

  return (
    <div className="p-6 max-w-6xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-4">
        <div className="flex items-center gap-3">
          <Clock className="text-blue-600" size={26} />
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Analiza czasu pracy</h1>
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

      {/* Alert niefakturowanych */}
      {s && s.unbilledOlderThan30 > 0 && (
        <div className="flex items-start gap-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-300 dark:border-amber-700 rounded-xl p-4">
          <AlertTriangle className="text-amber-500 flex-shrink-0 mt-0.5" size={20} />
          <div>
            <p className="text-sm font-semibold text-amber-800 dark:text-amber-200">
              {s.unbilledOlderThan30} {s.unbilledOlderThan30 === 1 ? 'wpis' : 'wpisów'} niezafakturowanych od ponad 30 dni
            </p>
            {data && data.unbilledAlerts.slice(0, 5).map(a => (
              <p key={a.entryId} className="text-xs text-amber-700 dark:text-amber-300 mt-1">
                {a.clientName} / {a.projectName} — {fmtH(a.hours)} ({a.startedAt}, {a.daysAgo} dni temu)
              </p>
            ))}
            {s.unbilledOlderThan30 > 5 && (
              <p className="text-xs text-amber-600 dark:text-amber-400 mt-1">…i {s.unbilledOlderThan30 - 5} więcej</p>
            )}
          </div>
        </div>
      )}

      {isLoading && (
        <div className="text-center py-16 text-gray-500 dark:text-gray-400">Ładowanie danych…</div>
      )}
      {isError && (
        <div className="text-center py-16 text-red-500">Błąd ładowania danych.</div>
      )}

      {data && (
        <>
          {/* KPI Cards */}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
            {[
              {
                label: 'Łącznie godzin',
                value: fmtH(s!.totalHours),
                sub: `${fmtH(s!.avgHoursPerWeek)} / tydzień`,
              },
              {
                label: 'Zafakturowane',
                value: fmtH(s!.invoicedHours),
                sub: `${s!.invoicedPercent}% całości`,
                color: 'text-blue-600 dark:text-blue-400',
              },
              {
                label: 'Niezafakturowane',
                value: fmtH(s!.pendingHours),
                sub: `${(100 - Number(s!.invoicedPercent)).toFixed(1)}% całości`,
                color: s!.pendingHours > 0 ? 'text-amber-600 dark:text-amber-400' : undefined,
              },
              {
                label: 'Śr. dzień roboczy',
                value: fmtH(s!.avgHoursPerWorkedDay),
                sub: `${s!.workedDays} dni z wpisami`,
              },
            ].map(({ label, value, sub, color }) => (
              <div key={label} className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
                <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">{label}</div>
                <div className={`text-xl font-bold ${color ?? 'text-gray-900 dark:text-gray-100'}`}>{value}</div>
                <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">{sub}</div>
              </div>
            ))}
          </div>

          {/* Wskaźnik urlopowy */}
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-5">
            <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">Tracker urlopu / odpoczynku</h2>
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-center">
              {[
                { label: 'Dni robocze w okresie', value: s!.businessDaysInPeriod, note: 'pon–pt' },
                { label: 'Dni z wpisami', value: s!.workedDays, note: 'aktywne' },
                {
                  label: 'Dni wolne (szacunek)',
                  value: freeDays,
                  note: `standard: ${standardVacation} dni`,
                  highlight: vacationStatus,
                },
                {
                  label: 'Pozostało do standardu',
                  value: Math.max(0, standardVacation - freeDays),
                  note: freeDays >= standardVacation ? '✓ Cel osiągnięty' : 'dni urlopu',
                  highlight: freeDays >= standardVacation ? 'ok' : undefined,
                },
              ].map(({ label, value, note, highlight }) => (
                <div key={label} className="flex flex-col items-center">
                  <div
                    className={`text-3xl font-bold mb-1 ${
                      highlight === 'ok' ? 'text-emerald-600 dark:text-emerald-400'
                      : highlight === 'warning' ? 'text-amber-500 dark:text-amber-400'
                      : highlight === 'low' ? 'text-red-500 dark:text-red-400'
                      : 'text-gray-900 dark:text-gray-100'
                    }`}
                  >
                    {value}
                  </div>
                  <div className="text-xs font-medium text-gray-600 dark:text-gray-300">{label}</div>
                  <div className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{note}</div>
                </div>
              ))}
            </div>

            {/* Pasek progresu urlopu */}
            <div className="mt-4">
              <div className="flex justify-between text-xs text-gray-500 dark:text-gray-400 mb-1">
                <span>Dni wolne: {freeDays} / {standardVacation} standardowych</span>
                <span>{Math.min(100, Math.round(freeDays / standardVacation * 100))}%</span>
              </div>
              <div className="h-2 bg-gray-100 dark:bg-gray-800 rounded-full overflow-hidden">
                <div
                  className={`h-2 rounded-full transition-all ${
                    vacationStatus === 'ok' ? 'bg-emerald-500'
                    : vacationStatus === 'warning' ? 'bg-amber-400'
                    : 'bg-red-400'
                  }`}
                  style={{ width: `${Math.min(100, freeDays / standardVacation * 100)}%` }}
                />
              </div>
            </div>
          </div>

          {/* Trend miesięczny */}
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-6">
            <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">
              Godziny per miesiąc — zafakturowane vs. niezafakturowane
            </h2>
            <ResponsiveContainer width="100%" height={220}>
              <ComposedChart data={chartData} margin={{ top: 4, right: 8, bottom: 0, left: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                <YAxis tickFormatter={v => `${v}h`} tick={{ fontSize: 11 }} width={36} />
                <Tooltip formatter={(v) => fmtH(Number(v))} />
                <Legend wrapperStyle={{ fontSize: 12 }} />
                <Bar dataKey="Zafakturowane" stackId="a" fill="#3b82f6" radius={[0, 0, 0, 0]} />
                <Bar dataKey="Niezafakturowane" stackId="a" fill="#fbbf24" radius={[4, 4, 0, 0]} />
              </ComposedChart>
            </ResponsiveContainer>
          </div>

          {/* Heatmap */}
          {data.days.length > 0 && (
            <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-6">
              <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">
                Intensywność pracy — {year}
              </h2>
              <WorkHeatmap days={data.days} year={year} />
            </div>
          )}

          {/* Tabela miesięczna */}
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden">
            <div className="px-4 py-3 border-b border-gray-100 dark:border-gray-800">
              <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300">Szczegóły miesięczne</h2>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="text-xs text-gray-500 dark:text-gray-400 border-b border-gray-100 dark:border-gray-800">
                    <th className="px-4 py-2 text-left">Miesiąc</th>
                    <th className="px-4 py-2 text-right">Razem</th>
                    <th className="px-4 py-2 text-right">Zafakturowane</th>
                    <th className="px-4 py-2 text-right">Niezafakturowane</th>
                    <th className="px-4 py-2 text-right">% zafakturowanych</th>
                    <th className="px-4 py-2 text-right">Dni</th>
                  </tr>
                </thead>
                <tbody>
                  {data.months.map(m => (
                    <tr key={m.month} className="border-b border-gray-50 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-4 py-2.5 text-sm font-medium text-gray-800 dark:text-gray-200">{m.monthName}</td>
                      <td className="px-4 py-2.5 text-sm text-right text-gray-700 dark:text-gray-300">{m.totalHours > 0 ? fmtH(m.totalHours) : '—'}</td>
                      <td className="px-4 py-2.5 text-sm text-right text-blue-600 dark:text-blue-400">{m.invoicedHours > 0 ? fmtH(m.invoicedHours) : '—'}</td>
                      <td className="px-4 py-2.5 text-sm text-right text-amber-600 dark:text-amber-400">{m.pendingHours > 0 ? fmtH(m.pendingHours) : '—'}</td>
                      <td className="px-4 py-2.5 text-sm text-right">
                        {m.totalHours > 0 ? (
                          <span className={m.invoicedPercent >= 80 ? 'text-emerald-600 dark:text-emerald-400' : 'text-gray-500 dark:text-gray-400'}>
                            {m.invoicedPercent}%
                          </span>
                        ) : '—'}
                      </td>
                      <td className="px-4 py-2.5 text-sm text-right text-gray-500 dark:text-gray-400">{m.workedDays > 0 ? m.workedDays : '—'}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr className="bg-gray-50 dark:bg-gray-800/50 font-semibold text-sm border-t-2 border-gray-200 dark:border-gray-700">
                    <td className="px-4 py-3 text-gray-700 dark:text-gray-300">Suma</td>
                    <td className="px-4 py-3 text-right text-gray-900 dark:text-gray-100">{fmtH(s!.totalHours)}</td>
                    <td className="px-4 py-3 text-right text-blue-600 dark:text-blue-400">{fmtH(s!.invoicedHours)}</td>
                    <td className="px-4 py-3 text-right text-amber-600 dark:text-amber-400">{fmtH(s!.pendingHours)}</td>
                    <td className="px-4 py-3 text-right text-gray-900 dark:text-gray-100">{s!.invoicedPercent}%</td>
                    <td className="px-4 py-3 text-right text-gray-900 dark:text-gray-100">{s!.workedDays}</td>
                  </tr>
                </tfoot>
              </table>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
