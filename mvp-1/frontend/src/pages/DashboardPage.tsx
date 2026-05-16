import { useQuery } from '@tanstack/react-query'
import { getTimeEntries } from '../api/timeEntries'
import { getInvoices } from '../api/invoices'
import { getClients } from '../api/clients'
import { TimerWidget } from '../components/timer/TimerWidget'

export function DashboardPage() {
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
  const totalRevenue = acceptedInvoices.reduce((acc, i) => acc + i.totalGross, 0)

  return (
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
          <div className="text-2xl font-bold text-green-600">{totalRevenue.toLocaleString('pl-PL', { maximumFractionDigits: 0 })} PLN</div>
        </div>
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
  )
}
