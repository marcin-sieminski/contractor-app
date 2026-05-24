import { useState, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getTimeEntries } from '../api/timeEntries'
import { getInvoices } from '../api/invoices'
import { getClients } from '../api/clients'
import { getExpenses } from '../api/expenses'
import { TimerWidget } from '../components/timer/TimerWidget'
import { RevenueByClientChart } from '../components/dashboard/RevenueByClientChart'
import { RevenueByMonthChart } from '../components/dashboard/RevenueByMonthChart'
import { ExpensesByMonthChart } from '../components/dashboard/ExpensesByMonthChart'
import { ExpensesByCategoryChart } from '../components/dashboard/ExpensesByCategoryChart'
import { ExpenseMonthDetailModal } from '../components/dashboard/ExpenseMonthDetailModal'
import { ExpenseCategoryDetailModal } from '../components/dashboard/ExpenseCategoryDetailModal'
import { MonthDetailModal } from '../components/dashboard/MonthDetailModal'
import { ClientDetailModal } from '../components/dashboard/ClientDetailModal'
import type { RevenueDataPoint } from '../types/revenueData'

type Mode = 'invoiced' | 'all'

export function DashboardPage() {
  const [mode, setMode] = useState<Mode>('invoiced')
  const [selectedMonth, setSelectedMonth] = useState<string | null>(null)
  const [selectedClient, setSelectedClient] = useState<string | null>(null)
  const [selectedExpenseMonth, setSelectedExpenseMonth] = useState<string | null>(null)
  const [selectedExpenseCategory, setSelectedExpenseCategory] = useState<string | null>(null)

  const { data: entries = [] } = useQuery({ queryKey: ['timeEntries'], queryFn: () => getTimeEntries() })
  const { data: invoices = [] } = useQuery({ queryKey: ['invoices'], queryFn: () => getInvoices() })
  const { data: clients = [] } = useQuery({ queryKey: ['clients'], queryFn: getClients })
  const { data: expenses = [] } = useQuery({ queryKey: ['expenses'], queryFn: () => getExpenses() })

  // ── godziny bieżącego miesiąca ────────────────────────────────────────
  const thisMonthMinutes = entries.filter(e => {
    const d = new Date(e.startedAt)
    const now = new Date()
    return d.getMonth() === now.getMonth() && d.getFullYear() === now.getFullYear() && !e.isRunning
  }).reduce((acc, e) => acc + (e.durationMinutes ?? 0), 0)

  const pendingInvoices = invoices.filter(i => i.status === 'Draft').length
  const acceptedInvoices = invoices.filter(i => i.status === 'Accepted')

  // ── mapa stawek projektów: projectId → {hourlyRate, currency} ─────────
  const projectRateMap = useMemo(() => {
    const map = new Map<string, { hourlyRate: number; currency: string }>()
    for (const client of clients)
      for (const project of client.projects)
        map.set(project.id, { hourlyRate: project.hourlyRate, currency: project.currency })
    return map
  }, [clients])

  // ── najnowszy kurs NBP per waluta (proxy z faktur) ───────────────────
  const exchangeRateMap = useMemo(() => {
    const map = new Map<string, number>()
    const sorted = [...invoices].sort((a, b) => b.issueDate.localeCompare(a.issueDate))
    for (const inv of sorted)
      if (inv.currency !== 'PLN' && inv.exchangeRate && !map.has(inv.currency))
        map.set(inv.currency, inv.exchangeRate)
    return map
  }, [invoices])

  function toPlnEstimate(projectId: string, durationMinutes: number): number {
    const proj = projectRateMap.get(projectId)
    if (!proj || !durationMinutes) return 0
    const nativeValue = (durationMinutes / 60) * proj.hourlyRate
    if (proj.currency === 'PLN') return nativeValue
    const rate = exchangeRateMap.get(proj.currency)
    return rate ? nativeValue * rate : 0
  }

  // ── dane wykresów: zafakturowane ─────────────────────────────────────
  const invoicedPoints: RevenueDataPoint[] = acceptedInvoices.map(inv => ({
    clientName: inv.clientName,
    monthKey: inv.issueDate.slice(0, 7),
    valuePLN: inv.currency === 'PLN'
      ? inv.totalGross
      : inv.exchangeRate ? inv.totalGross * inv.exchangeRate : 0,
  }))

  // ── dane wykresów: niezafakturowane ──────────────────────────────────
  const uninvoicedEntries = entries.filter(e => !e.isInvoiced && !e.isRunning && !e.isPaused)
  const uninvoicedPoints: RevenueDataPoint[] = uninvoicedEntries.map(e => ({
    clientName: e.clientName,
    monthKey: e.startedAt.slice(0, 7),
    valuePLN: toPlnEstimate(e.projectId, e.durationMinutes ?? 0),
  }))

  const activePoints = mode === 'all'
    ? [...invoicedPoints, ...uninvoicedPoints]
    : invoicedPoints

  // ── KPIs ─────────────────────────────────────────────────────────────
  const totalRevenuePLN = invoicedPoints.reduce((acc, p) => acc + p.valuePLN, 0)
  const totalExpensesPLN = expenses.reduce((acc, e) => acc + e.amountPLN, 0)
  const missingRateCount = acceptedInvoices.filter(i => i.currency !== 'PLN' && !i.exchangeRate).length

  const uninvoicedMinutes = uninvoicedEntries.reduce((acc, e) => acc + (e.durationMinutes ?? 0), 0)
  const uninvoicedPLN = uninvoicedPoints.reduce((acc, p) => acc + p.valuePLN, 0)
  const uninvoicedForeignCount = uninvoicedEntries.filter(e => {
    const proj = projectRateMap.get(e.projectId)
    return proj && proj.currency !== 'PLN' && !exchangeRateMap.has(proj.currency)
  }).length

  return (
    <>
    <div className="p-6">
      <h1 className="text-2xl font-bold mb-6">Dashboard</h1>

      <div className="mb-6">
        <TimerWidget />
      </div>

      {/* KPI cards */}
      <div className="grid grid-cols-5 gap-4 mb-6">
        <div className="bg-white border border-gray-200 rounded-xl p-4">
          <div className="text-gray-500 text-sm mb-1">Godziny (ten miesiąc)</div>
          <div className="text-2xl font-bold text-blue-600">{(thisMonthMinutes / 60).toFixed(1)}h</div>
        </div>
        <div className="bg-white border border-gray-200 rounded-xl p-4">
          <div className="text-gray-500 text-sm mb-1">Faktury do wysłania</div>
          <div className="text-2xl font-bold text-orange-500">{pendingInvoices}</div>
        </div>
        <div className="bg-white border border-gray-200 rounded-xl p-4">
          <div className="text-gray-500 text-sm mb-1">Łączny przychód (KSeF)</div>
          <div className="text-2xl font-bold text-green-600">
            {totalRevenuePLN.toLocaleString('pl-PL', { maximumFractionDigits: 0 })} PLN
          </div>
          {missingRateCount > 0 && (
            <div className="text-xs text-amber-600 mt-1">
              {missingRateCount} {missingRateCount === 1 ? 'faktura' : 'faktury'} bez kursu NBP
            </div>
          )}
          {acceptedInvoices.some(i => i.currency !== 'PLN' && i.exchangeRate) && (
            <div className="text-xs text-gray-400 mt-1">wg kursu NBP z dnia faktury</div>
          )}
        </div>
        <div className="bg-white border border-gray-200 rounded-xl p-4">
          <div className="text-gray-500 text-sm mb-1">Niezafakturowane</div>
          <div className="text-2xl font-bold text-violet-600">
            {uninvoicedPLN > 0
              ? uninvoicedPLN.toLocaleString('pl-PL', { maximumFractionDigits: 0 }) + ' PLN'
              : '—'}
          </div>
          <div className="text-xs text-gray-400 mt-1">
            {(uninvoicedMinutes / 60).toFixed(1)}h bez faktury
          </div>
          {uninvoicedForeignCount > 0 && (
            <div className="text-xs text-amber-600 mt-0.5">
              {uninvoicedForeignCount} {uninvoicedForeignCount === 1 ? 'wpis' : 'wpisy'} bez kursu pominięte
            </div>
          )}
        </div>
        <div className="bg-white border border-gray-200 rounded-xl p-4">
          <div className="text-gray-500 text-sm mb-1">Łączne wydatki</div>
          <div className="text-2xl font-bold text-red-600">
            {totalExpensesPLN > 0
              ? totalExpensesPLN.toLocaleString('pl-PL', { maximumFractionDigits: 0 }) + ' PLN'
              : '—'}
          </div>
          <div className="text-xs text-gray-400 mt-1">{expenses.length} {expenses.length === 1 ? 'wpis' : 'wpisów'}</div>
        </div>
      </div>

      {/* Przełącznik trybu wykresów */}
      <div className="flex items-center justify-between mb-3">
        <span className="text-sm font-semibold text-gray-700">Przychody</span>
        <div className="flex rounded-lg border border-gray-200 overflow-hidden text-sm">
          <button
            onClick={() => setMode('invoiced')}
            className={`px-4 py-1.5 font-medium transition-colors ${
              mode === 'invoiced'
                ? 'bg-blue-600 text-white'
                : 'bg-white text-gray-600 hover:bg-gray-50'
            }`}
          >
            Zafakturowane
          </button>
          <button
            onClick={() => setMode('all')}
            className={`px-4 py-1.5 font-medium transition-colors border-l border-gray-200 ${
              mode === 'all'
                ? 'bg-blue-600 text-white'
                : 'bg-white text-gray-600 hover:bg-gray-50'
            }`}
          >
            Wszystkie (+ szacowane)
          </button>
        </div>
      </div>

      {/* Wykresy */}
      <div className="grid grid-cols-2 gap-4 mb-6">
        <RevenueByMonthChart
          data={activePoints}
          selectedMonth={selectedMonth}
          onMonthClick={key => setSelectedMonth(key)}
        />
        <RevenueByClientChart
          data={activePoints}
          selectedClient={selectedClient}
          onClientClick={name => setSelectedClient(name)}
        />
      </div>

      {/* Sekcja wydatków */}
      <div className="flex items-center justify-between mb-3">
        <span className="text-sm font-semibold text-gray-700">Wydatki</span>
      </div>
      <div className="grid grid-cols-2 gap-4 mb-6">
        <ExpensesByMonthChart
          data={expenses}
          selectedMonth={selectedExpenseMonth}
          onMonthClick={key => setSelectedExpenseMonth(key)}
        />
        <ExpensesByCategoryChart
          data={expenses}
          selectedCategory={selectedExpenseCategory}
          onCategoryClick={label => setSelectedExpenseCategory(label)}
        />
      </div>

      <div className="bg-white border border-gray-200 rounded-xl p-4">
        <div className="text-sm font-semibold text-gray-700 mb-2">Klienci ({clients.length})</div>
        {clients.length === 0
          ? <div className="text-gray-400 text-sm">Brak klientów</div>
          : clients.map(c => (
              <div key={c.id} className="flex justify-between py-1 text-sm border-b border-gray-50 last:border-0">
                <span>{c.name}</span>
                <span className="text-gray-500">{c.projects.length} projektów</span>
              </div>
            ))
        }
      </div>
    </div>

    {selectedMonth && (
      <MonthDetailModal
        monthKey={selectedMonth}
        entries={entries}
        onClose={() => setSelectedMonth(null)}
      />
    )}
    {selectedClient && (
      <ClientDetailModal
        clientName={selectedClient}
        entries={entries}
        onClose={() => setSelectedClient(null)}
      />
    )}
    {selectedExpenseMonth && (
      <ExpenseMonthDetailModal
        monthKey={selectedExpenseMonth}
        expenses={expenses}
        onClose={() => setSelectedExpenseMonth(null)}
      />
    )}
    {selectedExpenseCategory && (
      <ExpenseCategoryDetailModal
        categoryLabel={selectedExpenseCategory}
        expenses={expenses}
        onClose={() => setSelectedExpenseCategory(null)}
      />
    )}
    </>
  )
}
