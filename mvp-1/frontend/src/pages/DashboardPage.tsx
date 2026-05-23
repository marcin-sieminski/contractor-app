import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getTimeEntries } from '../api/timeEntries'
import { getInvoices } from '../api/invoices'
import { getClients } from '../api/clients'
import { TimerWidget } from '../components/timer/TimerWidget'
import { RevenueByClientChart } from '../components/dashboard/RevenueByClientChart'
import { RevenueByMonthChart } from '../components/dashboard/RevenueByMonthChart'
import { MonthDetailModal } from '../components/dashboard/MonthDetailModal'
import { ClientDetailModal } from '../components/dashboard/ClientDetailModal'

export function DashboardPage() {
  const [selectedMonth, setSelectedMonth] = useState<string | null>(null)
  const [selectedClient, setSelectedClient] = useState<string | null>(null)
  const { data: entries = [] } = useQuery({ queryKey: ['timeEntries'], queryFn: () => getTimeEntries() })
  const { data: invoices = [] } = useQuery({ queryKey: ['invoices'], queryFn: () => getInvoices() })
  const { data: clients = [] } = useQuery({ queryKey: ['clients'], queryFn: getClients })

  const thisMonthMinutes = entries
    .filter(e => {
      const d = new Date(e.startedAt)
      const now = new Date()
      return d.getMonth() === now.getMonth() && d.getFullYear() === now.getFullYear() && !e.isRunning
    })
    .reduce((acc, e) => acc + (e.durationMinutes ?? 0), 0)

  const pendingInvoices = invoices.filter(i => i.status === 'Draft').length
  const acceptedInvoices = invoices.filter(i => i.status === 'Accepted')

  const totalRevenuePLN = acceptedInvoices.reduce((acc, i) => {
    if (i.currency === 'PLN') return acc + i.totalGross
    if (i.exchangeRate) return acc + i.totalGross * i.exchangeRate
    return acc
  }, 0)

  const missingRateCount = acceptedInvoices.filter(
    i => i.currency !== 'PLN' && !i.exchangeRate
  ).length

  return (
    <>
    <div className="p-6">
      <h1 className="text-2xl font-bold mb-6">Dashboard</h1>

      <div className="mb-6">
        <TimerWidget />
      </div>

      <div className="grid grid-cols-3 gap-4 mb-6">
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
              {missingRateCount} {missingRateCount === 1 ? 'faktura walutowa bez kursu NBP' : 'faktury walutowe bez kursu NBP'}
            </div>
          )}
          {acceptedInvoices.some(i => i.currency !== 'PLN' && i.exchangeRate) && (
            <div className="text-xs text-gray-400 mt-1">wg kursu NBP z dnia faktury</div>
          )}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4 mb-6">
        <RevenueByMonthChart
          invoices={invoices}
          selectedMonth={selectedMonth}
          onMonthClick={key => setSelectedMonth(key)}
        />
        <RevenueByClientChart
          invoices={invoices}
          selectedClient={selectedClient}
          onClientClick={name => setSelectedClient(name)}
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
    </>
  )
}
