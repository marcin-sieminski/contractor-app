import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer
} from 'recharts'
import { AlertTriangle, Download, TrendingUp, Clock, DollarSign, Percent } from 'lucide-react'
import { getIpBoxProgress, setTimeEntryIpWork } from '../api/ipBox'
import api from '../api/client'
import type { UnfilledIpEntryDto } from '../types/ipBox'

const fmtPln = (v: number) =>
  new Intl.NumberFormat('pl-PL', { style: 'currency', currency: 'PLN', maximumFractionDigits: 0 }).format(v)
const fmtH = (v: number) => `${v.toFixed(1)} h`
const fmtPct = (v: number) => `${v.toFixed(1)}%`

async function downloadCsv(year: number, month?: number) {
  const params = month ? `?month=${month}` : ''
  const res = await api.get(`/ip-box/${year}/export-csv${params}`, { responseType: 'blob' })
  const url = URL.createObjectURL(new Blob([res.data], { type: 'text/csv;charset=utf-8;' }))
  const a = document.createElement('a')
  a.href = url
  a.download = month ? `ipbox_${year}_${String(month).padStart(2, '0')}.csv` : `ipbox_${year}.csv`
  a.click()
  URL.revokeObjectURL(url)
}

function KpiCard({ label, value, sub, icon: Icon, color }: {
  label: string; value: string; sub?: string; icon: React.ElementType; color: string
}) {
  return (
    <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-5 flex items-start gap-4">
      <div className={`p-2 rounded-lg ${color}`}>
        <Icon size={20} className="text-white" />
      </div>
      <div>
        <div className="text-xs text-gray-500 dark:text-gray-400">{label}</div>
        <div className="text-xl font-bold text-gray-900 dark:text-gray-100 mt-0.5">{value}</div>
        {sub && <div className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{sub}</div>}
      </div>
    </div>
  )
}

function UnfilledRow({ entry, onFill }: { entry: UnfilledIpEntryDto; onFill: (id: string) => void }) {
  return (
    <tr className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
      <td className="px-4 py-2 text-sm text-gray-500 dark:text-gray-400">{entry.startedAt}</td>
      <td className="px-4 py-2 text-sm font-medium text-gray-900 dark:text-gray-100">{entry.projectName}</td>
      <td className="px-4 py-2 text-sm text-gray-600 dark:text-gray-300 max-w-xs truncate">{entry.description}</td>
      <td className="px-4 py-2 text-sm text-gray-500 dark:text-gray-400">
        {entry.durationMinutes ? fmtH(entry.durationMinutes / 60) : '—'}
      </td>
      <td className="px-4 py-2">
        <button
          onClick={() => onFill(entry.timeEntryId)}
          className="text-xs px-3 py-1 rounded bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 hover:bg-blue-200 dark:hover:bg-blue-900/50 transition-colors"
        >
          Uzupełnij opis
        </button>
      </td>
    </tr>
  )
}

export function IpBoxTrackerPage() {
  const currentYear = new Date().getFullYear()
  const [year, setYear] = useState(currentYear)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editDesc, setEditDesc] = useState('')
  const qc = useQueryClient()

  const { data, isLoading, error } = useQuery({
    queryKey: ['ip-box', year],
    queryFn: () => getIpBoxProgress(year),
  })

  const saveIpWork = useMutation({
    mutationFn: ({ id, desc }: { id: string; desc: string }) =>
      setTimeEntryIpWork(id, true, desc),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['ip-box'] })
      setEditingId(null)
      setEditDesc('')
    },
  })

  const years = Array.from({ length: 4 }, (_, i) => currentYear - i)

  const chartData = data?.monthlyData.map(m => ({
    name: m.monthName.slice(0, 3),
    'Godziny IP': Number(m.ipHours.toFixed(1)),
    'Godziny inne': Number((m.totalHours - m.ipHours).toFixed(1)),
  })) ?? []

  const revenueData = data?.monthlyData.map(m => ({
    name: m.monthName.slice(0, 3),
    'IP kwalifikowany': m.qualifyingRevenuePln,
    'Pozostały': m.totalRevenuePln - m.ipRevenuePln,
  })) ?? []

  return (
    <div className="p-6 space-y-6 max-w-7xl mx-auto">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">IP Box Progress Dashboard</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            Nexus coefficient: {data?.nexusCoefficient ?? 1.0} · Stawka IP Box: 5% (vs. 19% liniowy)
          </p>
        </div>
        <div className="flex items-center gap-3">
          <select
            aria-label="Rok"
            value={year}
            onChange={e => setYear(Number(e.target.value))}
            className="border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
          >
            {years.map(y => <option key={y} value={y}>{y}</option>)}
          </select>
          <button
            onClick={() => downloadCsv(year)}
            className="flex items-center gap-2 px-4 py-2 bg-green-600 hover:bg-green-700 text-white text-sm rounded-lg transition-colors"
          >
            <Download size={16} aria-hidden="true" />
            Eksportuj CSV
          </button>
        </div>
      </div>

      {isLoading && (
        <div className="text-center py-20 text-gray-500 dark:text-gray-400">Ładowanie danych IP Box…</div>
      )}

      {error && (
        <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl p-4 text-red-700 dark:text-red-400 text-sm">
          Błąd ładowania danych: {(error as Error).message}
        </div>
      )}

      {data && (
        <>
          {/* KPI */}
          <div className="grid grid-cols-2 xl:grid-cols-4 gap-4">
            <KpiCard
              label="Godziny IP / ogółem"
              value={`${fmtH(data.ipHours)} / ${fmtH(data.totalHours)}`}
              sub={fmtPct(data.ipHoursPercent) + ' czasu pracy'}
              icon={Clock}
              color="bg-blue-500"
            />
            <KpiCard
              label="Przychód kwalifikowany"
              value={fmtPln(data.qualifyingRevenuePln)}
              sub={`z ${fmtPln(data.totalRevenuePln)} ogółem`}
              icon={DollarSign}
              color="bg-purple-500"
            />
            <KpiCard
              label="Oszczędności podatkowe"
              value={fmtPln(data.taxSavingsPln)}
              sub="14% × przychód kwalifikowany"
              icon={TrendingUp}
              color="bg-green-500"
            />
            <KpiCard
              label="Prognoza roczna oszczędności"
              value={fmtPln(data.projectedYearSavingsPln)}
              sub="ekstrapolacja do XII"
              icon={Percent}
              color="bg-orange-500"
            />
          </div>

          {/* IP ratio alert */}
          {data.ipHoursPercent < 50 && (
            <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700 rounded-xl p-4 flex items-start gap-3">
              <AlertTriangle className="text-amber-500 shrink-0 mt-0.5" size={18} />
              <div>
                <div className="font-medium text-amber-800 dark:text-amber-300 text-sm">
                  Udział godzin IP poniżej 50%
                </div>
                <div className="text-amber-700 dark:text-amber-400 text-xs mt-1">
                  Urząd skarbowy może kwestionować IP Box gdy większość czasu nie jest związana z tworzeniem oprogramowania.
                  Upewnij się, że wpisy IP są prawidłowo oznaczone i opisane.
                </div>
              </div>
            </div>
          )}

          {/* Charts */}
          <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
            <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
              <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">Godziny IP vs. inne (miesięcznie)</h2>
              <ResponsiveContainer width="100%" height={220}>
                <BarChart data={chartData} barSize={14}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                  <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                  <YAxis tick={{ fontSize: 11 }} unit="h" />
                  <Tooltip formatter={(v) => `${Number(v).toFixed(1)} h`} />
                  <Legend wrapperStyle={{ fontSize: 12 }} />
                  <Bar dataKey="Godziny IP" stackId="a" fill="#6366f1" />
                  <Bar dataKey="Godziny inne" stackId="a" fill="#e5e7eb" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>

            <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
              <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">Przychód kwalifikowany IP Box (PLN)</h2>
              <ResponsiveContainer width="100%" height={220}>
                <BarChart data={revenueData} barSize={14}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                  <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                  <YAxis tick={{ fontSize: 11 }} tickFormatter={v => `${(Number(v) / 1000).toFixed(0)}k`} />
                  <Tooltip formatter={(v) => fmtPln(Number(v))} />
                  <Legend wrapperStyle={{ fontSize: 12 }} />
                  <Bar dataKey="IP kwalifikowany" stackId="a" fill="#10b981" />
                  <Bar dataKey="Pozostały" stackId="a" fill="#e5e7eb" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>

          {/* Monthly table */}
          <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700">
            <div className="px-5 py-4 border-b border-gray-200 dark:border-gray-700">
              <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300">Tabela miesięczna</h2>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-xs text-gray-500 dark:text-gray-400">
                    <th className="px-4 py-3 text-left">Miesiąc</th>
                    <th className="px-4 py-3 text-right">Godz. IP</th>
                    <th className="px-4 py-3 text-right">Godz. ogółem</th>
                    <th className="px-4 py-3 text-right">% IP</th>
                    <th className="px-4 py-3 text-right">Przychód ogółem</th>
                    <th className="px-4 py-3 text-right">Przychód IP</th>
                    <th className="px-4 py-3 text-right">Kwalifikowany</th>
                    <th className="px-4 py-3" />
                  </tr>
                </thead>
                <tbody>
                  {data.monthlyData.map(m => {
                    const pct = m.totalHours > 0 ? (m.ipHours / m.totalHours * 100) : 0
                    return (
                      <tr key={m.month} className="border-b border-gray-100 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-800/30">
                        <td className="px-4 py-3 font-medium text-gray-900 dark:text-gray-100">{m.monthName}</td>
                        <td className="px-4 py-3 text-right text-gray-700 dark:text-gray-300">{fmtH(m.ipHours)}</td>
                        <td className="px-4 py-3 text-right text-gray-500 dark:text-gray-400">{fmtH(m.totalHours)}</td>
                        <td className="px-4 py-3 text-right">
                          <span className={`font-medium ${pct >= 50 ? 'text-green-600 dark:text-green-400' : 'text-amber-600 dark:text-amber-400'}`}>
                            {fmtPct(pct)}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-right text-gray-700 dark:text-gray-300">{fmtPln(m.totalRevenuePln)}</td>
                        <td className="px-4 py-3 text-right text-indigo-600 dark:text-indigo-400">{fmtPln(m.ipRevenuePln)}</td>
                        <td className="px-4 py-3 text-right text-green-600 dark:text-green-400 font-medium">{fmtPln(m.qualifyingRevenuePln)}</td>
                        <td className="px-4 py-3 text-right">
                          {m.ipHours > 0 && (
                            <button
                              onClick={() => downloadCsv(year, m.month)}
                              aria-label="Eksportuj CSV miesiąca"
                              title="Eksportuj CSV miesiąca"
                              className="text-gray-500 hover:text-gray-700 dark:hover:text-gray-200"
                            >
                              <Download size={14} aria-hidden="true" />
                            </button>
                          )}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
                <tfoot>
                  <tr className="bg-gray-50 dark:bg-gray-800 font-semibold text-sm">
                    <td className="px-4 py-3 text-gray-900 dark:text-gray-100">Suma</td>
                    <td className="px-4 py-3 text-right text-gray-900 dark:text-gray-100">{fmtH(data.ipHours)}</td>
                    <td className="px-4 py-3 text-right text-gray-900 dark:text-gray-100">{fmtH(data.totalHours)}</td>
                    <td className="px-4 py-3 text-right">
                      <span className={data.ipHoursPercent >= 50 ? 'text-green-600 dark:text-green-400' : 'text-amber-600 dark:text-amber-400'}>
                        {fmtPct(data.ipHoursPercent)}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right text-gray-900 dark:text-gray-100">{fmtPln(data.totalRevenuePln)}</td>
                    <td className="px-4 py-3 text-right text-indigo-600 dark:text-indigo-400">{fmtPln(data.ipRevenuePln)}</td>
                    <td className="px-4 py-3 text-right text-green-600 dark:text-green-400">{fmtPln(data.qualifyingRevenuePln)}</td>
                    <td />
                  </tr>
                </tfoot>
              </table>
            </div>
          </div>

          {/* Unfilled entries */}
          {data.unfilledEntries.length > 0 && (
            <div className="bg-white dark:bg-gray-900 rounded-xl border border-red-200 dark:border-red-800">
              <div className="px-5 py-4 border-b border-red-200 dark:border-red-800 flex items-center gap-2">
                <AlertTriangle size={16} className="text-red-500" />
                <h2 className="text-sm font-semibold text-red-700 dark:text-red-400">
                  {data.unfilledEntries.length} wpis(-ów) IP bez opisu kwalifikującego
                </h2>
              </div>

              {editingId && (
                <div className="px-5 py-4 border-b border-gray-200 dark:border-gray-700 bg-blue-50 dark:bg-blue-900/20">
                  <div className="text-xs font-medium text-gray-600 dark:text-gray-400 mb-2">
                    Opis kwalifikujący (co konkretnie tworzyłeś — algorytm, API, komponent):
                  </div>
                  <div className="flex gap-2">
                    <input
                      aria-label="Opis kwalifikujący"
                      value={editDesc}
                      onChange={e => setEditDesc(e.target.value)}
                      placeholder="np. Implementacja algorytmu rekomendacji w module AI, wytworzenie kodu źródłowego"
                      className="flex-1 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
                    />
                    <button
                      onClick={() => saveIpWork.mutate({ id: editingId, desc: editDesc })}
                      disabled={!editDesc.trim() || saveIpWork.isPending}
                      className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white rounded-lg transition-colors"
                    >
                      Zapisz
                    </button>
                    <button
                      onClick={() => setEditingId(null)}
                      className="px-4 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800"
                    >
                      Anuluj
                    </button>
                  </div>
                </div>
              )}

              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-gray-100 dark:border-gray-800 text-xs text-gray-500 dark:text-gray-400">
                      <th className="px-4 py-3 text-left">Data</th>
                      <th className="px-4 py-3 text-left">Projekt</th>
                      <th className="px-4 py-3 text-left">Opis zadania</th>
                      <th className="px-4 py-3 text-right">Czas</th>
                      <th className="px-4 py-3" />
                    </tr>
                  </thead>
                  <tbody>
                    {data.unfilledEntries.map(e => (
                      <UnfilledRow key={e.timeEntryId} entry={e} onFill={id => { setEditingId(id); setEditDesc('') }} />
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {data.unfilledEntries.length === 0 && (
            <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-700 rounded-xl p-4 text-sm text-green-700 dark:text-green-400 flex items-center gap-2">
              <span className="text-xl">✓</span>
              Wszystkie wpisy IP mają uzupełnione opisy kwalifikujące.
            </div>
          )}
        </>
      )}
    </div>
  )
}
