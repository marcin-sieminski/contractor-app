import { NavLink, Outlet } from 'react-router-dom'
import { Clock, Users, FileText, LayoutDashboard } from 'lucide-react'

const nav = [
  { to: '/', icon: LayoutDashboard, label: 'Dashboard' },
  { to: '/time', icon: Clock, label: 'Czas pracy' },
  { to: '/clients', icon: Users, label: 'Klienci' },
  { to: '/invoices', icon: FileText, label: 'Faktury' }
]

export function AppShell() {
  return (
    <div className="flex h-screen">
      <aside className="w-56 bg-white border-r border-gray-200 flex flex-col p-4 gap-1">
        <div className="font-bold text-xl text-blue-600 mb-6 px-2">ContractorApp</div>
        {nav.map(({ to, icon: Icon, label }) => (
          <NavLink
            key={to}
            to={to}
            end={to === '/'}
            className={({ isActive }) =>
              `flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                isActive ? 'bg-blue-50 text-blue-700' : 'text-gray-600 hover:bg-gray-100'
              }`
            }
          >
            <Icon size={18} />
            {label}
          </NavLink>
        ))}
      </aside>
      <main className="flex-1 overflow-auto">
        <Outlet />
      </main>
    </div>
  )
}
